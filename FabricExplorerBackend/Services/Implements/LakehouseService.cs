using FabricExplorerBackend.Commons;
using FabricModels = Microsoft.Fabric.Api.Lakehouse.Models;
using System.Security.Claims;
using FabricExplorerBackend.Services.Interfaces;
using FabricExplorerBackend.Repositories.Interfaces;
using Microsoft.Fabric.Api.Core.Models;
using Microsoft.Fabric.Api.Lakehouse.Models;
using FabricExplorerBackend.Securities;

namespace FabricExplorerBackend.Services.Implements
{
    public class LakehouseService(
        IConfiguration configuration,
        IHttpContextAccessor httpContext,
        IFabricContextFactory fabricContextFactory) : ILakehouseService
    {
        public async Task<ApiResponse<PagedResponse<Lakehouse>>> GetAllLakehouse(paginationRequest pageInformation)
        {
            //var connectionId = httpContext.HttpContext?.Request.Headers
            //    .FirstOrDefault(h => h.Key == configuration.GetValue<string>("Headers:Connection")).Value;

            //var context = await fabricContextFactory.CreateFabricContextAsync(Guid.Parse(connectionId!));
            //var lakehouses = context?.Client.Lakehouse.Items.ListLakehouses(context.WorkspaceId);

            throw new NotImplementedException();
        }
    }
}
