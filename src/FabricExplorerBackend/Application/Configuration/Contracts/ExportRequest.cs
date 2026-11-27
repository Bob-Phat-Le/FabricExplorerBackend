namespace FabricExplorerBackend.Application.Configuration.Contracts
{
    public sealed class ExportRequest
    {
        /// <summary>"all" or "selected".</summary>
        public string? Scope { get; set; }
        public List<ExportItemRef>? Items { get; set; }
    }
}
