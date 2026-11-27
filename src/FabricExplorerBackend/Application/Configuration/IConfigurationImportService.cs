using FabricExplorerBackend.Application.Configuration.Contracts;
using FabricExplorerBackend.Commons.Results;

namespace FabricExplorerBackend.Application.Configuration
{
    public interface IConfigurationImportService
    {
        /// <summary>Dry run: validates and matches the file against the current data. Writes nothing.</summary>
        Task<Result<PreviewResponse>> PreviewAsync(PreviewRequest request, CancellationToken cancellationToken = default);

        Task<Result<ImportResponse>> ImportAsync(ImportRequest request, string? idempotencyKey, CancellationToken cancellationToken = default);
    }
}
