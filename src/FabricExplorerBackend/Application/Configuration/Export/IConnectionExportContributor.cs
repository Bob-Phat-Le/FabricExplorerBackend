using FabricExplorerBackend.Application.Configuration.Documents;
using ConnectionEntity = FabricExplorerBackend.Domain.Entities.Connection;

namespace FabricExplorerBackend.Application.Configuration.Export
{
    /// <summary>Adds extra information to an exported connection (extension point).</summary>
    public interface IConnectionExportContributor
    {
        Task ContributeAsync(ConnectionEntity connection, ConnectionEntry entry, ExportContext context, CancellationToken cancellationToken);
    }
}
