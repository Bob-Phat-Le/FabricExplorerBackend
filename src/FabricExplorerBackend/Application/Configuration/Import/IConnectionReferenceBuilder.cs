using FabricExplorerBackend.Application.Configuration.Documents;
using FabricExplorerBackend.Commons.Results;

namespace FabricExplorerBackend.Application.Configuration.Import
{
    /// <summary>Works out which connection every key of the file stands for.</summary>
    public interface IConnectionReferenceBuilder
    {
        Task<Result<ConnectionReferences>> BuildAsync(
            ConfigurationDocument document, IReadOnlyDictionary<string, Guid>? mappings, CancellationToken cancellationToken);
    }
}
