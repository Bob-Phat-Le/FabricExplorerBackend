using FabricExplorerBackend.Application.QueryService.Contracts;
using FabricExplorerBackend.Commons.Contracts;
using FabricExplorerBackend.Commons.EventStreaming;
using FabricExplorerBackend.Commons.Results;

namespace FabricExplorerBackend.Application.QueryService
{
    public interface IQueryService
    {
        /// <summary>Creates the query metadata and queues the execution. Does not wait for the SQL to run.</summary>
        Task<Result<QueryAcceptedResponse>> CreateQueryAsync(Guid userId, QueryRequest request, CancellationToken cancellationToken = default);

        Task<Result<QueryResponse>> GetQueryAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

        Task<Result<List<QueryResponse>>> GetQueriesAsync(Guid userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

        Task<Result<QueryResultPageResponse>> GetResultPageAsync(Guid userId, Guid id, long offset, int limit, CancellationToken cancellationToken = default);

        Task<Result<QueryResponse>> CancelQueryAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

        /// <summary>Deletes the stored result and the metadata of a finished query.</summary>
        Task<Result<ConfirmationResponse>> DeleteQueryAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

        /// <summary>Lifecycle (status) stream of one query. Never carries rows.</summary>
        Task<Result<EventStream>> OpenStreamAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);

        void CloseStream(EventStream stream);
    }
}
