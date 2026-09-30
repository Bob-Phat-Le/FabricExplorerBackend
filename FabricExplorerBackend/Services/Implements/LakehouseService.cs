using FabricExplorerBackend.Commons;
using FabricModels = Microsoft.Fabric.Api.Lakehouse.Models;
using System.Security.Claims;
using FabricExplorerBackend.Services.Interfaces;
using FabricExplorerBackend.Repositories.Interfaces;
using Microsoft.Fabric.Api.Core.Models;
using Microsoft.Fabric.Api.Lakehouse.Models;

namespace FabricExplorerBackend.Services.Implements
{
    public class LakehouseService(
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IHttpContextAccessor httpContext,
        IFabricContextFactory fabricContextFactory) : ILakehouseService
    {
        public async Task<ApiResponse<PagedResponse<Lakehouse>>> GetAllLakehouse(PageInformation pageInformation)
        {
            throw new NotImplementedException();
        }
    }
}
