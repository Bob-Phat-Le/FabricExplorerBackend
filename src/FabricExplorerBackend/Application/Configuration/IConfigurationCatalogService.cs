using FabricExplorerBackend.Application.Configuration.Contracts;
using FabricExplorerBackend.Commons.Paginations;
using FabricExplorerBackend.Commons.Results;

namespace FabricExplorerBackend.Application.Configuration
{
    public interface IConfigurationCatalogService
    {
        Result<ConfigurationCapabilitiesResponse> GetCapabilities();
        Task<Result<PagedResponse<IReadOnlyList<ConfigurationItemResponse>>>> GetItemsAsync(ConfigurationItemsQuery query, CancellationToken cancellationToken = default);
    }
}
