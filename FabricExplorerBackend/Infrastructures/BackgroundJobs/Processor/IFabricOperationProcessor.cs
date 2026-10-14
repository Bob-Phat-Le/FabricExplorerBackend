namespace FabricExplorerBackend.Infrastructures.Workers.Processor
{
    public interface IFabricOperationProcessor
    {
        Task PollAsync(Guid operationId, CancellationToken ct);
    }
}
