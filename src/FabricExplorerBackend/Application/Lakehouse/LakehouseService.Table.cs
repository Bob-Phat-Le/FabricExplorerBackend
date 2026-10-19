using FabricExplorerBackend.Application.Lakehouse;
using FabricExplorerBackend.Application.Lakehouse.Contracts;
using FabricExplorerBackend.Application.Operation;
using FabricExplorerBackend.Application.Operation.Contracts;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.Paginations;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Infrastructures.Fabric.Dtos;
using System.Diagnostics;

namespace FabricExplorerBackend.Features.Fabric.Lakehouse
{
    public partial class LakehouseService
    {
        public async Task<Result<PagedResponseWithCursor<IEnumerable<TableResponse>>>> GetAllTablesAsync(
            Guid connectionId,
            Guid workspaceId,
            Guid lakehouseId,
            PaginationWithCursorRequest? paginationRequest,
            bool bypassCache = false,
            CancellationToken cancellationToken = default)
        {
            if (paginationRequest != null)
                paginationRequest.Validate();

            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection == null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

            var client = fabricRestClientFactory.CreateFabricRestClient(connection);

            var pageSize = paginationRequest?.PageSize ?? 20;
            var continuationToken = paginationRequest?.ContinuationToken;
            var isFirstPage = string.IsNullOrWhiteSpace(continuationToken);

            var url =
                $"{fabricOptions.Api.BaseUrl}" +
                $"/workspaces/{workspaceId}" +
                $"/lakehouses/{lakehouseId}/tables" +
                $"?pageSize={pageSize}";
            if (!isFirstPage)
                url += $"&continuationToken={Uri.EscapeDataString(continuationToken!)}";

            var response = await client.GetAsync<FabricValueListResponse<FabricTableResponse>>(url, cancellationToken);

            var allTables = response?.Data ?? [];
            IEnumerable<FabricTableResponse> tables = allTables!;

            var pageInformation = new PageInformationWithCursor
            {
                PageSize = pageSize,
                NextToken = response?.ContinuationToken,
                ItemsCount = allTables.Count
            };

            var statsByTable = await tableStatsService.GetStatsAsync(
                connection, workspaceId, lakehouseId, tables, bypassCache, cancellationToken);

            var result = tables
                .Select(t =>
                {
                    var item = lakehouseMapper.Map(t);
                    var stats = statsByTable.GetValueOrDefault(t.Name);

                    item.RowCount = stats?.RowCount;
                    item.LastModifiedTime = stats?.LastModifiedTime;
                    item.Status = (stats?.Status ?? TableStatus.Unknown).ToString();
                    item.StatusMessage = stats?.StatusMessage;
                    return item;
                })
                .ToList();

            return Result<PagedResponseWithCursor<IEnumerable<TableResponse>>>.Success(
                new PagedResponseWithCursor<IEnumerable<TableResponse>>(result, pageInformation));
        }

        public async Task<Result<OperationAcceptedResponse>> LoadTableAsync(
            Guid connectionId, Guid workspaceId, Guid lakehouseId, string tableName,
            ApplicationLoadTableRequest request, CancellationToken cancellationToken = default)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection is null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found.");

            var context = await fabricContextFactory.CreateFabricContextWithoutTokenAsync(connection, cancellationToken);

            var loadTableRequest = lakehouseMapper.Map(request);

            var response = await context.Client.Lakehouse.Tables.LoadTableAsync(
                workspaceId, lakehouseId, tableName, loadTableRequest, cancellationToken: CancellationToken.None);

            if (response.Status != StatusCodes.Status202Accepted)
                return Result<OperationAcceptedResponse>.Success(new(null, "Table load completed."));

            response.Headers.TryGetValue("x-ms-operation-id", out var fabricOperationId);
            response.Headers.TryGetValue("Location", out var location);

            try
            {
                var operationRequest = fabricOperationMapper.Map(response);
                operationRequest.OperationType = FabricOperationTypes.LoadTable;
                operationRequest.ConnectionId = connectionId;
                operationRequest.WorkspaceId = workspaceId;
                operationRequest.ResourceId = lakehouseId;
                operationRequest.ResourceName = tableName;

                var created = await fabricOperationService.CreateFabricOperationAsync(operationRequest, cancellationToken);

                return Result<OperationAcceptedResponse>.Success(new(
                    created.Data!.Id,
                    "Load table accepted. Poll GET /api/operations/{operationId} until the status is final."));
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "LoadTable was accepted by Fabric but the operation could not be tracked. " +
                    "FabricOperationId={FabricOperationId} Location={Location} Table={Table} Lakehouse={LakehouseId}",
                    fabricOperationId, location, tableName, lakehouseId);

                return Result<OperationAcceptedResponse>.Success(new(
                    null,
                    "Load table was accepted by Fabric but its progress cannot be tracked. Check the table status later."));
            }
        }
    }
}
