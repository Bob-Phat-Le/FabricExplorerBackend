using FabricExplorerBackend.Commons;
using FabricModels = Microsoft.Fabric.Api.Lakehouse.Models;
using System.Security.Claims;
using FabricExplorerBackend.Services.Interfaces;
using FabricExplorerBackend.Repositories.Interfaces;
using Microsoft.Fabric.Api.Core.Models;
using Microsoft.Fabric.Api.Lakehouse.Models;
using FabricExplorerBackend.Securities;
using FabricExplorerBackend.Models.Responses;
using Microsoft.Fabric.Api.Utils;
using FabricExplorerBackend.Mappers.FabricOperation;
using FabricExplorerBackend.Enums;

namespace FabricExplorerBackend.Services.Implements
{
    public class LakehouseService(
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        IHttpContextAccessor httpContext,
        IFabricContextFactory fabricContextFactory,
        IFabricOperationMapper fabricOperationMapper,
        IFabricOperationService fabricOperationService) : ILakehouseService
    {
        public async Task<Result<PagedResponse<IEnumerable<Table>>>> GetAllTablesAsync(Guid connectionId, Guid lakehouseId, PaginationRequest? paginationRequest)
        {
            try
            {
                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
                if (connection == null)
                    return Result<PagedResponse<IEnumerable<Table>>>.Failure(Enums.ResultStatus.NotFound, "connection not found.");

                var context = await fabricContextFactory.CreateFabricContextAsync(connection);
                if (context == null)
                    return Result<PagedResponse<IEnumerable<Table>>>.Failure(Enums.ResultStatus.BadRequest, "failed to create fabric context.");

                var tables = await context.Client.Lakehouse.Tables.ListTablesAsync(connection.WorkspaceId, lakehouseId).ToListAsync();

                var pageInfomartion = new PageInformation();
                if (paginationRequest != null)
                {
                    pageInfomartion.Page = paginationRequest.Page;
                    pageInfomartion.PageSize = pageInfomartion.PageSize;
                    pageInfomartion.HasNextPage = tables.Count > paginationRequest.Page * paginationRequest.PageSize;

                    tables = tables
                        .Skip((paginationRequest.Page - 1) * paginationRequest.PageSize)
                        .Take(paginationRequest.PageSize)
                        .ToList();

                    pageInfomartion.Total = tables.Count;
                }

                return Result<PagedResponse<IEnumerable<Table>>>
                    .Success(new PagedResponse<IEnumerable<Table>>(tables, pageInfomartion));
            }
            catch (Exception ex)
            {
                return Result<PagedResponse<IEnumerable<Table>>>
                    .Failure(Enums.ResultStatus.InternalError, ex.Message);
            }
        }

        public async Task<Result<ConfirmationResponse>> LoadTableAsync(Guid connectionId, Guid lakehouseId, string tableName, LoadTableRequest request)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
            if (connection == null)
                return Result<ConfirmationResponse>.Failure(Enums.ResultStatus.NotFound, "connection not found.");

            var context = await fabricContextFactory.CreateFabricContextAsync(connection);
            if (context == null)
                return Result<ConfirmationResponse>.Failure(Enums.ResultStatus.BadRequest, "failed to create fabric context.");

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
