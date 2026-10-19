namespace FabricExplorerBackend.Application.MirroredDatabase.Contracts
{
    public class MirroringActionResponse
    {
        public string Message { get; set; } = null!;
        public Guid? OperationId { get; set; }
    }
}
