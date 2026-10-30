using FabricExplorerBackend.Application.QueryService.Contracts;
using FabricExplorerBackend.Commons.Contracts;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.EventStreaming;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.Infrastructures.Options;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace FabricExplorerBackend.Application.QueryService
{
    public class QueryService(
        IUnitOfWork unitOfWork,
        IQueryJobQueue jobQueue,
        IQueryResultReader resultReader,
        IQueryResultStore resultStore,
        IEventRegistry registry,
        IEventPublisher publisher,
        IOptions<QueryOptions> options,
        TimeProvider clock,
        ILogger<QueryService> logger) : IQueryService
    {
        private readonly QueryOptions opt = options.Value;

        public async Task<Result<QueryAcceptedResponse>> CreateQueryAsync(
            Guid userId, QueryRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
                return Error.BadRequest(ErrorCodes.Query.EmptySql, "SQL must not be empty.");
            if (request.Query.Length > opt.MaxSqlLength)
                return Error.BadRequest(ErrorCodes.Query.SqlTooLong, $"SQL must not exceed {opt.MaxSqlLength} characters.");
            if (request.Endpoint is null
                || string.IsNullOrWhiteSpace(request.Endpoint.Server)
                || string.IsNullOrWhiteSpace(request.Endpoint.Database))
                return Error.BadRequest(ErrorCodes.Query.InvalidEndpoint, "SQL endpoint server and database are required.");

            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(request.ConnectionId, cancellationToken: cancellationToken);
            if (connection is null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

            // 1) Metadata first. If this fails nothing is executed (there would be nothing to track the run).
            var query = QueryMetadata.Create(userId, connection.Id, request.Query, clock.GetUtcNow(), opt.ResultRetention);
            await unitOfWork.QueryMetadataRepository.AddAsync(query, cancellationToken);
            if (await unitOfWork.SaveChangesAsync(cancellationToken) <= 0)
                throw new InvalidOperationException($"Saving query {query.Id} affected 0 rows.");

            // 2) Hand the execution to the background workers. A single DB transaction cannot span
            //    PostgreSQL + Fabric SQL + result storage, so the lifecycle state is the source of truth.
            long? maxRows = request.MaxRows is > 0 ? request.MaxRows : null;
            var job = new QueryJob(query.Id, userId, connection.Id, request.Endpoint, maxRows);
            if (!jobQueue.TryEnqueue(job))
            {
                unitOfWork.QueryMetadataRepository.Delete(query);
                await unitOfWork.SaveChangesAsync(CancellationToken.None);
                return new Error(ErrorKind.Unavailable, ErrorCodes.Query.QueueFull, "Too many queries are waiting to run, retry later");
            }

            return Result<QueryAcceptedResponse>.Success(new QueryAcceptedResponse(query.Id, query.Status));
        }

        public async Task<Result<QueryResponse>> GetQueryAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
        {
            var query = await FindOwnedAsync(userId, id, cancellationToken);
            return query is null ? NotFound() : Result<QueryResponse>.Success(query.ToResponse());
        }

        public async Task<Result<List<QueryResponse>>> GetQueriesAsync(
            Guid userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
        {
            skip = Math.Max(0, skip);
            take = take is <= 0 or > 100 ? 50 : take;

            var (items, _) = await unitOfWork.QueryMetadataRepository.GetByUserIdAsync(userId, skip, take, false, cancellationToken);
            return Result<List<QueryResponse>>.Success(items.Select(q => q.ToResponse()).ToList());
        }

        public async Task<Result<QueryResultPageResponse>> GetResultPageAsync(
            Guid userId, Guid id, long offset, int limit, CancellationToken cancellationToken = default)
        {
            var query = await FindOwnedAsync(userId, id, cancellationToken);
            if (query is null)
                return NotFound();

            if (query.Status != QueryStatus.Completed)
                return Error.Conflict(ErrorCodes.Query.ResultNotReady,
                    $"The result is not available while the query is {query.Status}.");

            if (query.ExpiresAt <= clock.GetUtcNow())
                return Error.NotFound(ErrorCodes.Query.ResultExpired, "The result of this query has expired.");

            offset = Math.Max(0, offset);
            limit = limit <= 0 ? opt.DefaultPageSize : Math.Min(limit, opt.MaxPageSize);

            var page = await resultReader.ReadPageAsync(id, offset, limit, cancellationToken);
            if (page is null)
                return Error.NotFound(ErrorCodes.Query.ResultExpired, "The result of this query is no longer available.");

            return Result<QueryResultPageResponse>.Success(new QueryResultPageResponse(
                id, offset, limit, page.TotalRows, offset + page.Rows.Count < page.TotalRows,
                page.IsTruncated, page.Columns, page.Rows));
        }

        public async Task<Result<QueryResponse>> CancelQueryAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
        {
            var query = await FindOwnedAsync(userId, id, cancellationToken);
            if (query is null)
                return NotFound();

            if (query.IsTerminal)
                return Result<QueryResponse>.Success(query.ToResponse());

            // Still waiting in the queue: cancel it atomically so a worker can never start it.
            if (await unitOfWork.QueryMetadataRepository.TryMarkCancelledAsync(id, clock.GetUtcNow(), cancellationToken))
            {
                var cancelled = await unitOfWork.QueryMetadataRepository.GetByIdAsync(id, false, cancellationToken) ?? query;
                Publish(cancelled);
                return Result<QueryResponse>.Success(cancelled.ToResponse());
            }

            // Already started: signal the worker. It persists the Cancelled state and publishes the event itself.
            jobQueue.TryCancel(id);
            var current = await unitOfWork.QueryMetadataRepository.GetByIdAsync(id, false, cancellationToken) ?? query;
            return Result<QueryResponse>.Success(current.ToResponse());
        }

        public async Task<Result<ConfirmationResponse>> DeleteQueryAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
        {
            var query = await FindOwnedAsync(userId, id, cancellationToken);
            if (query is null)
                return NotFound();

            if (!query.IsTerminal)
                return Error.Conflict(ErrorCodes.Query.StillActive, "The query is still running, cancel it first.");

            // Files first, then the row: a failure in between leaves a row that the cleanup retries.
            await resultStore.DeleteAsync(id, cancellationToken);
            await unitOfWork.QueryMetadataRepository.DeleteByIdAsync(id, cancellationToken);

            return Result<ConfirmationResponse>.Success(new ConfirmationResponse { Message = "Query deleted" });
        }

        public async Task<Result<EventStream>> OpenStreamAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
        {
            // Register the stream BEFORE reading the status: a transition that happens in between is then
            // delivered by the publisher instead of being lost (worst case the client sees a duplicate).
            var stream = registry.Create(userId.ToString(), QueryStreamEvents.ChannelFor(id));
            try
            {
                var query = await FindOwnedAsync(userId, id, cancellationToken);
                if (query is null)
                {
                    registry.Remove(stream.Id);
                    return NotFound<EventStream>();
                }

                // Snapshot of the current state; terminal when the query already finished.
                publisher.Publish(stream.Id, QueryStreamEvents.EventNameFor(query.Status), query.ToStreamEvent());
            }
            catch
            {
                registry.Remove(stream.Id);
                throw;
            }

            return Result<EventStream>.Success(stream);
        }

        public void CloseStream(EventStream stream) => registry.Remove(stream.Id);

        private async Task<QueryMetadata?> FindOwnedAsync(Guid userId, Guid id, CancellationToken cancellationToken)
        {
            var query = await unitOfWork.QueryMetadataRepository.GetByIdAsync(id, false, cancellationToken);
            return query is not null && query.UserId == userId ? query : null;
        }

        private void Publish(QueryMetadata query)
        {
            try
            {
                publisher.PublishToOwner(
                    query.UserId.ToString(),
                    QueryStreamEvents.ChannelFor(query.Id),
                    QueryStreamEvents.EventNameFor(query.Status),
                    query.ToStreamEvent());
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cannot publish status of query {QueryId}", query.Id);
            }
        }

        private static Error NotFound() => Error.NotFound(ErrorCodes.Query.NotFound, "Query not found");

        private static Result<T> NotFound<T>() => NotFound();
    }
}
