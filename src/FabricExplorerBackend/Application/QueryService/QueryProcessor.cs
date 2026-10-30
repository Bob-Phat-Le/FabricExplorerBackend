using FabricExplorerBackend.Application.QueryService.Contracts;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Infrastructures.Options;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using CommonsApplicationException = FabricExplorerBackend.Commons.Errors.ApplicationException;

namespace FabricExplorerBackend.Application.QueryService
{
    /// <summary>
    /// Pending -> Running -> Completed | Failed | Cancelled.
    /// SQL execution and result storage cannot be rolled back by a PostgreSQL transaction, so every failure
    /// is recorded on the Query row and the partial result is deleted.
    /// </summary>
    public sealed class QueryProcessor(
        IUnitOfWork unitOfWork,
        IQueryExecutor executor,
        IQueryResultWriter writer,
        IQueryResultStore store,
        IQueryJobQueue jobQueue,
        Commons.EventStreaming.IEventPublisher publisher,
        IOptions<QueryOptions> options,
        TimeProvider clock,
        ILogger<QueryProcessor> logger) : IQueryProcessor
    {
        private readonly QueryOptions opt = options.Value;

        public async Task RunAsync(QueryJob job, CancellationToken stoppingToken)
        {
            var repository = unitOfWork.QueryMetadataRepository;

            // Register BEFORE claiming, so a cancel request can never fall between "claimed" and "cancellable".
            using var runCts = jobQueue.BeginRun(job.QueryId, stoppingToken);
            QueryMetadata? query = null;
            try
            {
                if (!await repository.TryMarkRunningAsync(job.QueryId, clock.GetUtcNow(), CancellationToken.None))
                    return; // cancelled while waiting in the queue, or already removed

                query = await repository.GetByIdAsync(job.QueryId, trackChanges: true, cancellationToken: CancellationToken.None);
                if (query is null)
                    return;

                Publish(query);

                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(job.ConnectionId, cancellationToken: runCts.Token);
                if (connection is null)
                    throw new CommonsApplicationException(Commons.Results.Error.NotFound(ErrorCodes.Connection.NotFound, "Connection no longer exists"));

                QueryResultMetadata result;
                await using (var execution = await executor.ExecuteAsync(connection, job.Endpoint, query.Sql, runCts.Token))
                    result = await writer.WriteAsync(job.QueryId, execution.Reader, job.MaxRows, runCts.Token);

                query.MarkCompleted(result.RowCount, clock.GetUtcNow(), opt.ResultRetention);
                await unitOfWork.SaveChangesAsync(CancellationToken.None);
                Publish(query);
            }
            // The SQL provider does not always surface cancellation as OperationCanceledException.
            catch (Exception) when (runCts.IsCancellationRequested)
            {
                await DeleteResultAsync(job.QueryId);

                if (stoppingToken.IsCancellationRequested)
                    await FinalizeAsync(job, query, failure: "The query was interrupted because the server is shutting down.");
                else
                    await FinalizeAsync(job, query, failure: null);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Query {QueryId} failed", job.QueryId);
                await DeleteResultAsync(job.QueryId);
                await FinalizeAsync(job, query, failure: Describe(ex));
            }
            finally
            {
                jobQueue.EndRun(job.QueryId);
            }
        }

        /// <summary>failure == null means "cancelled by the user".</summary>
        private async Task FinalizeAsync(QueryJob job, QueryMetadata? query, string? failure)
        {
            try
            {
                query ??= await unitOfWork.QueryMetadataRepository.GetByIdAsync(job.QueryId, trackChanges: true, cancellationToken: CancellationToken.None);
                if (query is null)
                    return;

                var now = clock.GetUtcNow();
                if (failure is null)
                    query.MarkCancelled(now);
                else
                    query.MarkFailed(failure, now);

                await unitOfWork.SaveChangesAsync(CancellationToken.None);
                Publish(query);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot persist the final state of query {QueryId}", job.QueryId);
            }
        }

        private async Task DeleteResultAsync(Guid queryId)
        {
            try
            {
                await store.DeleteAsync(queryId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                // The expiry cleanup removes whatever is left.
                logger.LogWarning(ex, "Cannot delete the partial result of query {QueryId}", queryId);
            }
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

        // The editor must show the real SQL error (syntax error, invalid object...), so SqlException keeps its message.
        private static string Describe(Exception ex) => ex switch
        {
            SqlException sql => sql.Message,
            CommonsApplicationException app => app.Error.Message,
            _ => ExceptionClassifier.Classify(ex).Message
        };
    }
}
