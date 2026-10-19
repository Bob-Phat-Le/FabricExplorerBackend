using FabricExplorerBackend.Application.Connection.Contracts;
using FabricExplorerBackend.UnitTests.Features.Connection.Abstraction;
using Moq;
using Entities = FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.UnitTests.Features.Connection
{
    public class GetActiveConnection_Tests : ConnectionServiceTestBase
    {
        [Fact]
        public async Task GetActiveConnectionAsync_WhenUserExists_ReturnsActiveConnection()
        {
            // Arrange
            var activeConnection = new Entities.Connection
            {
                Id = _connectionId1,
                Name = "Production Fabric"
            };

            var user = new Entities.User
            {
                Id = _userId,
                ActiveConnectionId = _connectionId1,
                ActiveConnection = activeConnection
            };

            var response = new ConnectionResponse();

            _unitOfWork
                .Setup(x => x.UserRepository.GetByIdWithConnectionAsync(
                    _userId,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _connectionMapper
                .Setup(x => x.Map(activeConnection))
                .Returns(response);

            // Act
            var result = await _service.GetActiveConnectionAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Same(response, result.Data);

            _connectionMapper.Verify(
                x => x.Map(activeConnection),
                Times.Once);
        }

        [Fact]
        public async Task GetActiveConnectionAsync_WhenUserDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            _unitOfWork
                .Setup(x => x.UserRepository.GetByIdWithConnectionAsync(
                    _userId,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Entities.User?)null);

            // Act
            var result = await _service.GetActiveConnectionAsync();

            // Assert
            Assert.False(result.IsSuccess);

            _connectionMapper.Verify(
                x => x.Map(It.IsAny<Entities.Connection>()),
                Times.Never);
        }
    }
}
