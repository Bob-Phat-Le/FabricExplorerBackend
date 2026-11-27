using FabricExplorerBackend.Application.Configuration.Documents;
using ConnectionEntity = FabricExplorerBackend.Domain.Entities.Connection;

namespace FabricExplorerBackend.Application.Configuration.Import
{
    /// <summary>Something done to a connection right after it was created or updated, saved together with it.</summary>
    public interface IConnectionImportStep
    {
        /// <returns>Flags to show on the result of the connection.</returns>
        Task<IReadOnlyList<string>> ApplyAsync(ConnectionEntity connection, ConnectionEntry entry, ImportSession session, CancellationToken cancellationToken);
    }
}
