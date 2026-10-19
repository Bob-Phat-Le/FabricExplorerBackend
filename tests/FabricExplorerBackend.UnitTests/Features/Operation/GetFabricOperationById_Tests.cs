using FabricExplorerBackend.Application.Operation.Contracts;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.UnitTests.Features.Operation.Abstraction;
using Moq;

namespace FabricExplorerBackend.UnitTests.Features.Operation
{
    public class GetFabricOperationById_Tests : FabricOperationServiceTestBase
    {
        [Fact]
        public async Task GetFabricOperationById_WhenOperationNotFound_ReturnsNotFoundError()
        {
            // Arrange
            _fabricOperationRepository
                .Setup(r => r.GetByIdAsync(_operationId, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync((FabricOperation?)null);

            // Act
            var result = await _service.GetFabricOperationByIdAsync(_connectionId, _operationId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains(result.Errors, e => e.Code == ErrorCodes.Operation.NotFound);
        }

        [Fact]
        public async Task GetFabricOperationById_WhenConnectionIdMismatch_ReturnsNotFoundError()
        {
            // Arrange
            var otherConnectionId = Guid.NewGuid();
            var operation = new FabricOperation
            {
                Id = _operationId,
                ConnectionId = otherConnectionId,
                FabricOperationId = Guid.NewGuid(),
                Status = FabricOperationStatus.Running
            };

            _fabricOperationRepository
                .Setup(r => r.GetByIdAsync(_operationId, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(operation);

            // Act
            var result = await _service.GetFabricOperationByIdAsync(_connectionId, _operationId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains(result.Errors, e => e.Code == ErrorCodes.Operation.NotFound);
        }

        [Fact]
        public async Task GetFabricOperationById_WhenFoundAndConnectionMatches_ReturnsSuccessResult()
        {
            // Arrange
            var operation = new FabricOperation
            {
                Id = _operationId,
                ConnectionId = _connectionId,
                FabricOperationId = Guid.NewGuid(),
                Status = FabricOperationStatus.Succeeded,
                PercentComplete = 100
            };

            var expectedResponse = new FabricOperationResponse
            {
                Id = _operationId,
                Status = FabricOperationStatus.Succeeded,
                PercentComplete = 100
            };

            _fabricOperationRepository
                .Setup(r => r.GetByIdAsync(_operationId, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(operation);

            _mapper
                .Setup(m => m.Map(operation))
                .Returns(expectedResponse);

            // Act
            var result = await _service.GetFabricOperationByIdAsync(_connectionId, _operationId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(_operationId, result.Data.Id);
            Assert.Equal(FabricOperationStatus.Succeeded, result.Data.Status);
            Assert.Equal(100, result.Data.PercentComplete);
        }
    }
}
