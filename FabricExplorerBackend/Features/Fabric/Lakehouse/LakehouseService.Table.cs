using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Requests.Paginations;
using FabricExplorerBackend.Commons.Models.Responses.Fabric;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.Exceptions;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricClient;
using FabricExplorerBackend.Commons.Models.Responses.Paginations;
using FabricExplorerBackend.Domain.Enums;
using Microsoft.Fabric.Api.Lakehouse.Models;

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
            try
            {
                if (paginationRequest != null)
                    paginationRequest.Validate();

                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
                if (connection == null)
                    return Result<PagedResponseWithCursor<IEnumerable<TableResponse>>>.Failure(ResultStatus.NotFound, "Connection not found.");

                var client = fabricRestClientFactory.CreateFabricRestClient(connection);

                // Workspace lấy từ route (không phải connection.WorkspaceId) để lakehouse ở workspace nào cũng đọc được
                var pageSize = paginationRequest?.PageSize ?? 20;
                var continuationToken = paginationRequest?.ContinuationToken;
                var isFirstPage = string.IsNullOrWhiteSpace(continuationToken);

                var url =
                    $"{configuration.GetValue<string>("External:BaseFabricApiUrl")}" +
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

                // Chỉ tính stats cho các bảng của trang hiện tại (có cache theo từng bảng); không bao giờ throw vì lỗi từng bảng
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (FabricErrorResponse ex)
            {
                return Result<PagedResponseWithCursor<IEnumerable<TableResponse>>>.Failure(ResultStatus.BadRequest, ex.Message);
            }
        }

        public async Task<Result<ConfirmationResponse>> LoadTableAsync(Guid connectionId, Guid lakehouseId, string tableName, LoadTableRequest request)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
            if (connection == null)
                return Result<ConfirmationResponse>.Failure(ResultStatus.NotFound, "connection not found.");

            var context = await fabricContextFactory.CreateFabricContextWithoutTokenAsync(connection);
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
