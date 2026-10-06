using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Requests.Paginations;
using FabricExplorerBackend.Commons.Models.Responses.Fabric;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricClient;
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

        public async Task<Result<ConfirmationResponse>> LoadTableAsync(Guid connectionId, Guid lakehouseId, string tableName, LoadTableRequest request)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
            if (connection == null)
                return Result<ConfirmationResponse>.Failure(ResultStatus.NotFound, "connection not found.");

            var context = await fabricContextFactory.CreateFabricContextAsync(connection);
            if (context == null)
                return Result<ConfirmationResponse>.Failure(ResultStatus.BadRequest, "failed to create fabric context.");

            var lroResponse = await context.Client.Lakehouse.Tables.LoadTableAsync(connection.WorkspaceId, lakehouseId, tableName, request);

            if (lroResponse.Status == StatusCodes.Status202Accepted)
            {
                var createOperationRequest = fabricOperationMapper.Map(lroResponse);
                var confirmation = await fabricOperationService.CreateFabricOperationAsync(createOperationRequest);

                var headers = request
                    .GetType()
                    .GetProperties()
                    .Where(p => p.GetValue(request) != null)
                    .ToDictionary(
                        p => p.Name,
                        p => p.GetValue(request)!.ToString()
                    );

                foreach (var (key, value) in headers)
                    httpContext.HttpContext?.Response?.Headers?.Add(key, value);
            }
            throw new NotImplementedException();
        }
    }
}
