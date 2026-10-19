using FabricExplorerBackend.UnitTests.Features.Connection.Abstraction;
using Moq;
using Entities = FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.UnitTests.Features.Connection
{
    public class ActiveConnection_Tests : ConnectionServiceTestBase
    {
        [Fact]
        public async Task ActiveConnection_WhenUserDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            _unitOfWork
                .Setup(x => x.UserRepository.GetByIdAsync(
                    _userId,
                    true,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Entities.User?)null);

            // Act
            var result = await _service.ActiveConnection(_connectionId2);

            // Assert
            Assert.False(result.IsSuccess);

            _unitOfWork.Verify(x =>
                x.ConnectionRepository.GetByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _unitOfWork.Verify(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ActiveConnection_WhenTargetConnectionDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var user = new Entities.User
            {
                Id = _userId
            };

            _unitOfWork
                .Setup(x => x.UserRepository.GetByIdAsync(
                    _userId,
                    true,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    _connectionId2,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Entities.Connection?)null);

            // Act
            var result = await _service.ActiveConnection(_connectionId2);

            // Assert
            Assert.False(result.IsSuccess);

            _unitOfWork.Verify(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ActiveConnection_WhenUserAndTargetExist_ReturnsSuccess()
        {
            // Arrange
            var user = new Entities.User
            {
                Id = _userId,
                ActiveConnectionId = _connectionId1
            };

            var targetConnection = new Entities.Connection
            {
                Id = _connectionId2
            };

            _unitOfWork
                .Setup(x => x.UserRepository.GetByIdAsync(
                    _userId,
                    true,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    _connectionId2,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(targetConnection);

            _unitOfWork
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.ActiveConnection(_connectionId2);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Active connection succeeded", result.Data!.Message);
            Assert.Equal(_connectionId2, user.ActiveConnectionId);

            _unitOfWork.Verify(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ActiveConnection_WhenSaveChangesReturnsZero_ThrowsInvalidOperationException()
        {
            // Arrange
            var user = new Entities.User
            {
                Id = _userId,
                ActiveConnectionId = _connectionId1
            };

            var targetConnection = new Entities.Connection
            {
                Id = _connectionId2
            };

            _unitOfWork
                .Setup(x => x.UserRepository.GetByIdAsync(
                    _userId,
                    true,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    _connectionId2,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(targetConnection);

            _unitOfWork
                .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ActiveConnection(_connectionId2));
        }
    }
}
