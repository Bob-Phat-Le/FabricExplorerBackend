using FabricExplorerBackend.Application.Operation.Contracts;
using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.UnitTests.Features.Operation.Abstraction;
using Moq;

namespace FabricExplorerBackend.UnitTests.Features.Operation
{
    public class CreateFabricOperation_Tests : FabricOperationServiceTestBase
    {
        [Fact]
        public async Task CreateFabricOperation_WhenSavingAffectsZeroRows_ThrowsInvalidOperationException()
        {
            // Arrange
            var request = new CreateFabricOperationRequest
            {
                OperationId = _operationId,
                OperationUrl = new Uri("https://api.fabric.microsoft.com/v1/operations/123"),
                OperationType = "StartMirroring",
                ConnectionId = _connectionId,
                WorkspaceId = _workspaceId,
                ResourceId = _resourceId,
                RetryAfter = TimeSpan.FromSeconds(5)
            };

            var entity = new FabricOperation
            {
                Id = Guid.NewGuid(),
                FabricOperationId = request.OperationId,
                FabricOperationUrl = request.OperationUrl.ToString(),
                OperationType = request.OperationType,
                ConnectionId = request.ConnectionId,
                WorkspaceId = request.WorkspaceId,
                ResourceId = request.ResourceId,
                Status = FabricOperationStatus.Pending
            };

            _mapper.Setup(m => m.Map(request)).Returns(entity);
            _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateFabricOperationAsync(request));

            Assert.Contains(request.OperationId.ToString(), ex.Message);
            _fabricOperationRepository.Verify(r => r.AddAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateFabricOperation_WhenSavingSucceeds_ReturnsSuccessResult()
        {
            // Arrange
            var request = new CreateFabricOperationRequest
            {
                OperationId = _operationId,
                OperationUrl = new Uri("https://api.fabric.microsoft.com/v1/operations/123"),
                OperationType = "StartMirroring",
                ConnectionId = _connectionId,
                WorkspaceId = _workspaceId,
                ResourceId = _resourceId,
                RetryAfter = TimeSpan.FromSeconds(5)
            };

            var entity = new FabricOperation
            {
                Id = Guid.NewGuid(),
                FabricOperationId = request.OperationId,
                FabricOperationUrl = request.OperationUrl.ToString(),
                OperationType = request.OperationType,
                ConnectionId = request.ConnectionId,
                WorkspaceId = request.WorkspaceId,
                ResourceId = request.ResourceId,
                Status = FabricOperationStatus.Pending
            };

            var expectedResponse = new FabricOperationResponse
            {
                Id = entity.Id,
                OperationType = entity.OperationType,
                ResourceId = entity.ResourceId,
                Status = entity.Status
            };

            _mapper.Setup(m => m.Map(request)).Returns(entity);
            _mapper.Setup(m => m.Map(entity)).Returns(expectedResponse);
            _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _service.CreateFabricOperationAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(expectedResponse.Id, result.Data.Id);
            Assert.Equal("StartMirroring", result.Data.OperationType);
            Assert.Equal(FabricOperationStatus.Pending, result.Data.Status);
            _fabricOperationRepository.Verify(r => r.AddAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
