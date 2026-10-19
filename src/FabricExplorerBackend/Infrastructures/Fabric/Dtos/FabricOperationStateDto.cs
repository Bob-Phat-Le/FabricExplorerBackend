namespace FabricExplorerBackend.Infrastructures.Fabric.Dtos
{
    public sealed class FabricOperationStateDto
    {
        public string? Status { get; set; }
        public int? PercentComplete { get; set; }
        public FabricOperationErrorDto? Error { get; set; }
    }
}
