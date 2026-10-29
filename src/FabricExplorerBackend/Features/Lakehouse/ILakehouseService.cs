using FabricExplorerBackend.Commons.EventStreaming;
using FabricExplorerBackend.Commons.Paginations;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Features.Lakehouse.Contracts;
using FabricExplorerBackend.Features.Operation.Contracts;

namespace FabricExplorerBackend.Features.Lakehouse
{
    public interface ILakehouseService
    {
        Task<Result<EventStreamingAdapter<PagedResponseWithCursor<IEnumerable<TableResponse>>>>> GetAllTablesAsync(
            Guid connectionId, Guid workspaceId, Guid lakehouseId, PaginationWithCursorRequest? paginationRequest,
            bool bypassCache = false, CancellationToken cancellationToken = default);
        Task<Result<OperationAcceptedResponse>> LoadTableAsync(
            Guid connectionId, Guid workspaceId, Guid lakehouseId, string tableName,
            ApplicationLoadTableRequest request, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<LakehouseListItemResponse>>> GetAllLakehousesAsync(
            Guid connectionId, Guid workspaceId, bool bypassCache = false, CancellationToken cancellationToken = default);
    }
}
