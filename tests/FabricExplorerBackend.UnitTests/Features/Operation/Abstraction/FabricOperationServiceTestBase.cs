using FabricExplorerBackend.Features.Operation;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Moq;

namespace FabricExplorerBackend.UnitTests.Features.Operation.Abstraction
{
    public abstract class FabricOperationServiceTestBase
    {
        protected readonly Mock<IUnitOfWork> _unitOfWork = new();
        protected readonly Mock<IFabricOperationRepository> _fabricOperationRepository = new();
        protected readonly Mock<IFabricOperationMapper> _mapper = new();

        protected readonly IFabricOperationService _service;

        protected readonly Guid _connectionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        protected readonly Guid _workspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        protected readonly Guid _resourceId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        protected readonly Guid _operationId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        protected FabricOperationServiceTestBase()
        {
            _unitOfWork
                .Setup(u => u.FabricOperationRepository)
                .Returns(_fabricOperationRepository.Object);

            _service = new FabricOperationService(
                _unitOfWork.Object,
                _mapper.Object);
        }
    }
}
