using FabricExplorerBackend.Application.Connection.Contracts;
using FabricExplorerBackend.UnitTests.Features.Connection.Abstraction;
using Moq;
using Entities = FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.UnitTests.Features.Connection
{
    public class GetConnectionById_Tests : ConnectionServiceTestBase
    {
        [Fact]
        public async Task GetConnectionByIdAsync_WhenConnectionExists_ReturnsSuccess()
        {
            // Arrange
            var response = new ConnectionResponse();

            _connectionMapper
                .Setup(x => x.Map(It.IsAny<Entities.Connection>()))
                .Returns(response);

            // Act
            var result = await _service.GetConnectionByIdAsync(_connectionId1);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Same(response, result.Data);

            _unitOfWork.Verify(x =>
                x.ConnectionRepository.GetByIdAsync(
                    _connectionId1,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetConnectionByIdAsync_WhenConnectionDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    id,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Entities.Connection?)null);

            // Act
            var result = await _service.GetConnectionByIdAsync(id);

            // Assert
            Assert.False(result.IsSuccess);

            _connectionMapper.Verify(
                x => x.Map(It.IsAny<Entities.Connection>()),
                Times.Never);
        }
    }
}
