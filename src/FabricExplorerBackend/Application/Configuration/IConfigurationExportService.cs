using FabricExplorerBackend.Application.Configuration.Contracts;
using FabricExplorerBackend.Commons.Results;

namespace FabricExplorerBackend.Application.Configuration
{
    public interface IConfigurationExportService
    {
        Task<Result<ConfigurationFile>> ExportAsync(ExportRequest request, CancellationToken cancellationToken = default);
    }
}
