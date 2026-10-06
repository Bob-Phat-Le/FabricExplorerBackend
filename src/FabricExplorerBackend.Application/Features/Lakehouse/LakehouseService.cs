using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Responses;
using FabricExplorerBackend.Commons.Models.Responses.FabricClient;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.Features.Fabric.Context;
using FabricExplorerBackend.Features.Fabric.FabricRestClient;
using FabricExplorerBackend.Features.Fabric.Operation;
using FabricExplorerBackend.Features.Mappers.FabricOperation;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.Fabric.Api.Lakehouse.Models;

namespace FabricExplorerBackend.Features.Fabric.Lakehouse
{
    public class LakehouseService(
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        IFabricRestClientFactory fabricRestClientFactory,
        IHttpContextAccessor httpContext,
        IFabricContextFactory fabricContextFactory,
        IFabricOperationMapper fabricOperationMapper,
        IFabricOperationService fabricOperationService) : ILakehouseService
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
                    };
                }

                var result = tables
                    .Select(MapTable)
                    .ToList();

                return Result<PagedResponseWithCursor<IEnumerable<TableResponse>>>.Success(
                    new PagedResponseWithCursor<IEnumerable<TableResponse>>(result, pageInformation));
            }
            catch (Exception ex)
            {
                return Result<PagedResponseWithCursor<IEnumerable<TableResponse>>>.Failure(ResultStatus.InternalError, ex.Message);
            }
        }

        private static TableResponse MapTable(FabricTableResponse table)
        {
            return new TableResponse
            {
                Type = table.Type,
                Name = table.Name,
                Location = table.Location,
                Format = table.Format
            };
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
