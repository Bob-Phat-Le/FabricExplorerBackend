using FabricExplorerBackend.Commons;
using Microsoft.Fabric.Api.Lakehouse.Models;
using FabricModels = Microsoft.Fabric.Api.Lakehouse.Models;

namespace FabricExplorerBackend.Services.Interfaces
{
    public interface ILakehouseService
    {
        Task<ApiResponse<PagedResponse<Lakehouse>>> GetAllLakehouse(PageInformation pageInformation);
    }
}
