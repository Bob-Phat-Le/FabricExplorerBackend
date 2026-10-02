using FabricExplorerBackend.Repositories.Interfaces;

namespace FabricExplorerBackend.Workers.Processor
{
    public class FabricOperationProcessor(
        IUnitOfWork unitOfWork) : IFabricOperationProcessor
    {
        public Task ProcessNotDoneOperationsAsync()
        {
            var operations = unitOfWork.FabricOperationRepository.GetAllNotDoneOperationsAsync();
            throw new NotImplementedException();
        }
    }
}
