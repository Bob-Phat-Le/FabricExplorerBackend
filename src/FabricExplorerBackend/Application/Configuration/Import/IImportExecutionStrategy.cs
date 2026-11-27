using FabricExplorerBackend.Application.Configuration.Contracts;

namespace FabricExplorerBackend.Application.Configuration.Import
{
    /// <summary>How the planned units are run: entry by entry, or all together.</summary>
    public interface IImportExecutionStrategy
    {
        bool Atomic { get; }
        Task<IReadOnlyList<ImportResultItem>> ExecuteAsync(IReadOnlyList<ImportUnit> units, ImportSession session, CancellationToken cancellationToken);
    }
}
