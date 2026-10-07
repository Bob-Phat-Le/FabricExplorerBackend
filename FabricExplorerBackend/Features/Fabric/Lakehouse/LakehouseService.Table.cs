using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Requests.Paginations;
using FabricExplorerBackend.Commons.Models.Responses.Fabric;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricClient;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricOperation;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.Paginations;
using FabricExplorerBackend.Domain.Enums;
using Microsoft.Fabric.Api.Lakehouse.Models;

namespace FabricExplorerBackend.Features.Fabric.Lakehouse
{
    public partial class LakehouseService
    {
        public async Task<Result<PagedResponseWithCursor<IEnumerable<TableResponse>>>> GetAllTablesAsync(Guid connectionId, Guid lakehouseId, PaginationWithCursorRequest? paginationRequest)
        {
            try
            {
                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
                if (connection == null)
                    return Result<PagedResponseWithCursor<IEnumerable<TableResponse>>>.Failure(ResultStatus.NotFound, "Connection not found.");

                var client = fabricRestClientFactory.CreateFabricRestClient(connection);

                var baseUrl =
                    $"{configuration.GetValue<string>("External:BaseFabricApiUrl")}" +
                    $"/workspaces/{connection.WorkspaceId}" +
                    $"/lakehouses/{lakehouseId}/tables" +
                    $"?pageSize={paginationRequest!.PageSize}";

                FabricListApiResponse<FabricTableResponse>? response;

                if (!string.IsNullOrWhiteSpace(paginationRequest?.ContinuationToken))
                {
                    var url =
                        $"{baseUrl}" +
                        $"&continuationToken={Uri.EscapeDataString(paginationRequest.ContinuationToken!)}";
                    response = await client.GetAsync<FabricListApiResponse<FabricTableResponse>>(url);
                }
                else
                    response = await client.GetAsync<FabricListApiResponse<FabricTableResponse>>(baseUrl);

                var allTables = response?.Data ?? [];
                IEnumerable<FabricTableResponse> tables = allTables!;

                PageInformationWithCursor pageInformation;
                if (!string.IsNullOrWhiteSpace(paginationRequest?.ContinuationToken))
                {
                    pageInformation = new PageInformationWithCursor
                    {
                        PageSize = paginationRequest.PageSize,
                        NextToken = response!.ContinuationToken,
                    };
                }
                else
                {
                    pageInformation = new PageInformationWithCursor
                    {
                        TotalItems = allTables.Count,
                        NextToken = response?.ContinuationToken
                    };
                }

                // Chỉ tính stats cho các bảng của trang hiện tại; không bao giờ throw vì lỗi từng bảng
                var statsByTable = await tableStatsService.GetStatsAsync(connection, lakehouseId, tables);

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
            catch (Exception ex)
            {
                return Result<PagedResponseWithCursor<IEnumerable<TableResponse>>>.Failure(ResultStatus.InternalError, ex.Message);
            }
        }

        public async Task<Result<FabricOperationResponse>> LoadTableAsync(Guid connectionId, Guid lakehouseId, string tableName, LoadTableRequest request)
        {
            try
            {
                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
                if (connection == null)
                    return Result<FabricOperationResponse>.Failure(ResultStatus.NotFound, "Connection not found.");

                var context = await fabricContextFactory.CreateFabricContextAsync(connection);
                if (context == null)
                    return Result<FabricOperationResponse>.Failure(ResultStatus.BadRequest, "Failed to create fabric context.");

                // Fabric nhận yêu cầu và trả 202 kèm các header của long running operation
                var lroResponse = await context.Client.Lakehouse.Tables.LoadTableAsync(connection.WorkspaceId, lakehouseId, tableName, request);

                if (lroResponse.Status != StatusCodes.Status202Accepted)
                    return Result<FabricOperationResponse>.Failure(
                        ResultStatus.InternalError, $"Unexpected response from Fabric (HTTP {lroResponse.Status}).");

                // Lưu operation để frontend hỏi trạng thái qua GET /api/operations/{id}
                var createOperationRequest = fabricOperationMapper.Map(lroResponse);
                createOperationRequest.ConnectionId = connectionId;
                createOperationRequest.OperationType = "LoadTable";
                createOperationRequest.ResourceId = $"{lakehouseId}/{tableName}";

                return await fabricOperationService.CreateFabricOperationAsync(createOperationRequest);
            }
            catch (Exception ex)
            {
                return Result<FabricOperationResponse>.Failure(ResultStatus.InternalError, ex.Message);
            }
        }
    }
}
