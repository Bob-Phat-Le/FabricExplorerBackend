namespace FabricExplorerBackend.Infrastructures.Workers.Processor
{
    public interface IFabricOperationProcessor
    {
        Task ProcessNotDoneOperationsAsync();
    }
}
