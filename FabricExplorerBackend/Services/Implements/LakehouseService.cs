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

namespace FabricExplorerBackend.Services.Implements
{
    public class LakehouseService(
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        IHttpContextAccessor httpContext,
        IFabricContextFactory fabricContextFactory) : ILakehouseService
    {
        public async Task<ApiResponse<PagedResponse<IEnumerable<Table>>>> GetAllTablesAsync(Guid connectionId, Guid lakehouseId, PaginationRequest? paginationRequest)
        {
            try
            {
                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
                if (connection == null)
                    return new(StatusCodes.Status404NotFound, "connection not found.");

                var context = await fabricContextFactory.CreateFabricContextAsync(connection);
                if (context == null)
                    return new(StatusCodes.Status400BadRequest, "failed to create fabric context.");

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

                return new(StatusCodes.Status200OK, new PagedResponse<IEnumerable<Table>>(tables, pageInfomartion));
            }
            catch (Exception ex)
            {
                return new(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        public async Task<ApiResponse<ConfirmationResponse>> LoadTableAsync(Guid connectionId, Guid lakehouseId, string tableName, LoadTableRequest request)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
            if (connection == null)
                return new(StatusCodes.Status404NotFound, "connection not found.");

            var context = await fabricContextFactory.CreateFabricContextAsync(connection);
            if (context == null)
                return new(StatusCodes.Status400BadRequest, "failed to create fabric context.");

            //var response = await context.Client.Lakehouse.Tables.LoadTableAsync(connection.WorkspaceId, lakehouseId, tableName, request);
            throw new NotImplementedException();
        }
    }
}
