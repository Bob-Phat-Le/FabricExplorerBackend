using System.Text.Json;

namespace FabricExplorerBackend.Application.Configuration.Contracts
{
    public sealed class PreviewRequest
    {
        public JsonElement Document { get; set; }
    }
}
