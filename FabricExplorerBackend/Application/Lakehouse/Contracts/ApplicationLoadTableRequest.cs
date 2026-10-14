using System.Text.Json;

namespace FabricExplorerBackend.Application.Lakehouse.Contracts
{
    public sealed class ApplicationLoadTableRequest
    {
        public string RelativePath { get; set; } = null!;
        public string PathType { get; set; } = null!;
        public string FileExtension { get; set; } = null!;
        public string Mode { get; set; } = null!;
        public bool Recursive { get; set; }
        public JsonElement FormatOptions { get; set; }
    }
}
