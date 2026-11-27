using FabricExplorerBackend.Application.Configuration.Documents;
using FabricExplorerBackend.Application.Configuration.Import;

namespace FabricExplorerBackend.Application.Configuration.Handlers
{
    public sealed class PreviewContext
    {
        public required Guid UserId { get; init; }
        public required ConfigurationDocument Document { get; init; }
        public required ConnectionReferences References { get; init; }
    }
}
