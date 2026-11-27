using FabricExplorerBackend.Application.Configuration.Contracts;
using FabricExplorerBackend.Application.Configuration.Documents;

namespace FabricExplorerBackend.Application.Configuration.Import
{
    /// <summary>Writes one validated connection entry.</summary>
    public interface IConnectionImporter
    {
        Task<ImportResultItem> ImportAsync(ConnectionEntry entry, ImportSession session, CancellationToken cancellationToken);
    }
}
