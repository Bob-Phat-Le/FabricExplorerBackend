namespace FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricClient
{
    public class TableResponse
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;

        public long? RowCount { get; set; }
        public DateTimeOffset? LastModifiedTime { get; set; }
        public string Status { get; set; } = null!;
        public string? StatusMessage { get; set; }
    }
}
