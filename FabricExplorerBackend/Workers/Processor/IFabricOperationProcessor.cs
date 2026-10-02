namespace FabricExplorerBackend.Workers.Processor
{
    public interface IFabricOperationProcessor
    {
        Task ProcessNotDoneOperationsAsync();
    }
}
