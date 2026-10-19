using FabricExplorerBackend.Application.Connection.Contracts;
using FabricExplorerBackend.UnitTests.Features.Connection.Abstraction;
using Moq;
using Entities = FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.UnitTests.Features.Connection
{
    public class UpdateConnection_Tests : ConnectionServiceTestBase
    {
        [Fact]
        public async Task UpdateConnection_WhenConnectionDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    id,
                    true,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Entities.Connection?)null);

            var request = new UpdateConnectionRequest();

            // Act
            var result = await _service.UpdateConnection(id, request);

            // Assert
            Assert.False(result.IsSuccess);

            _unitOfWork.Verify(x =>
                x.ConnectionRepository.Update(It.IsAny<Entities.Connection>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateConnection_WhenWorkspaceIsDuplicated_ReturnsConflict()
        {
            // Arrange
            var request = new UpdateConnectionRequest
            {
                WorkspaceId = _workspaceId2
            };

            var existingConnection = new Entities.Connection
            {
                Id = _connectionId1,
                WorkspaceId = _workspaceId1
            };

            var duplicatedConnection = new Entities.Connection
            {
                Id = _connectionId2,
                WorkspaceId = _workspaceId2
            };

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    _connectionId1,
                    true,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingConnection);

            _unitOfWork
                .Setup(x => x.ConnectionRepository.CheckDuplicateWorkspaceIdAsync(
                    _workspaceId2,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(duplicatedConnection);

            // Act
            var result = await _service.UpdateConnection(
                _connectionId1,
                request);

            // Assert
            Assert.False(result.IsSuccess);

            _unitOfWork.Verify(x =>
                x.ConnectionRepository.Update(It.IsAny<Entities.Connection>()),
                Times.Never);

            _unitOfWork.Verify(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateConnection_WhenValidRequest_ReturnsSuccess()
        {
            // Arrange
            var request = new UpdateConnectionRequest
            {
                WorkspaceId = _workspaceId1,
                ClientSecret = "new-secret"
            };

            var connection = new Entities.Connection
            {
                Id = _connectionId1,
                WorkspaceId = _workspaceId1,
                ClientSecret = "old-secret"
            };

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    _connectionId1,
                    true,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(connection);

            _secretProtector
                .Setup(x => x.Protect("new-secret"))
                .Returns("protected-new-secret");

            _connectionMapper
                .Setup(x => x.Map(request, connection));

            _unitOfWork
                .Setup(x => x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.UpdateConnection(
                _connectionId1,
                request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(
                "Connection updated successfully",
                result.Data!.Message);

            _connectionMapper.Verify(
                x => x.Map(request, connection),
                Times.Once);

            _unitOfWork.Verify(
                x => x.ConnectionRepository.Update(connection),
                Times.Once);

            _cacheService.Verify(
                 x => x.SetValueAsync(
                     It.IsAny<string>(),
                     It.IsAny<object>(),
                     It.IsAny<TimeSpan?>()),
                 Times.Once);
        }

        [Fact]
        public async Task UpdateConnection_WhenSaveChangesReturnsZero_ThrowsInvalidOperationException()
        {
            // Arrange
            var request = new UpdateConnectionRequest();

            var connection = new Entities.Connection
            {
                Id = _connectionId1,
                WorkspaceId = _workspaceId1
            };

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    _connectionId1,
                    true,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(connection);

            _unitOfWork
                .Setup(x => x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.UpdateConnection(
                    _connectionId1,
                    request));
        }
    }
}
