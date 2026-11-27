using FabricExplorerBackend.Application.Configuration.Contracts;
using FabricExplorerBackend.Commons.Results;

namespace FabricExplorerBackend.Application.Configuration
{
    public interface IConfigurationHistoryService
    {
        Task RecordAsync(
            Guid userId, string kind, string status, string? summary, int itemCount,
            string? idempotencyKey, ImportResponse? response, CancellationToken cancellationToken);

        /// <summary>The stored response of an import already done with this key, if any.</summary>
        Task<ImportResponse?> FindImportReplayAsync(Guid userId, string idempotencyKey, CancellationToken cancellationToken);

        Task<Result<IReadOnlyList<ConfigurationHistoryItem>>> GetRecentAsync(int? limit, CancellationToken cancellationToken);
    }
}
