using FabricExplorerBackend.Application.Connection.Contracts;
using FabricExplorerBackend.UnitTests.Features.Connection.Abstraction;
using Moq;
using Entities = FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.UnitTests.Features.Connection
{
    public class CreateConnection_Tests : ConnectionServiceTestBase
    {
        [Fact]
        public async Task CreateConnectionAsync_WhenValidRequest_ReturnsSuccess()
        {
            // Arrange
            var request = new CreateConnectionRequest
            {
                WorkspaceId = Guid.NewGuid(),
                ClientSecret = "my-secret"
            };

            var connection = new Entities.Connection
            {
                WorkspaceId = request.WorkspaceId,
                ClientSecret = request.ClientSecret
            };

            _connectionMapper
                .Setup(x => x.Map(request))
                .Returns(connection);

            _secretProtector
                .Setup(x => x.Protect(request.ClientSecret))
                .Returns("protected-secret");

            _unitOfWork
                .Setup(x => x.ConnectionRepository
                    .CheckDuplicateWorkspaceIdAsync(
                        request.WorkspaceId,
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync((Entities.Connection?)null);

            _unitOfWork
                .Setup(x => x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateConnectionAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(
                "Connection created successfully",
                result.Data!.Message);

            Assert.Equal("protected-secret", connection.ClientSecret);

            _unitOfWork.Verify(x =>
                x.ConnectionRepository.AddAsync(
                    connection,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _unitOfWork.Verify(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateConnectionAsync_WhenWorkspaceAlreadyExists_ReturnsConflict()
        {
            // Arrange
            var request = new CreateConnectionRequest
            {
                WorkspaceId = _workspaceId1,
                ClientSecret = "my-secret"
            };

            var connection = new Entities.Connection
            {
                WorkspaceId = request.WorkspaceId,
                ClientSecret = request.ClientSecret
            };

            _connectionMapper
                .Setup(x => x.Map(request))
                .Returns(connection);

            _secretProtector
                .Setup(x => x.Protect(request.ClientSecret))
                .Returns("protected-secret");

            _unitOfWork
                .Setup(x => x.ConnectionRepository
                    .CheckDuplicateWorkspaceIdAsync(
                        request.WorkspaceId,
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Entities.Connection
                {
                    Id = _connectionId1,
                    WorkspaceId = _workspaceId1
                });

            // Act
            var result = await _service.CreateConnectionAsync(request);

            // Assert
            Assert.False(result.IsSuccess);

            _unitOfWork.Verify(x =>
                x.ConnectionRepository.AddAsync(
                    It.IsAny<Entities.Connection>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _unitOfWork.Verify(x =>
                x.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateConnectionAsync_WhenSaveChangesReturnsZero_ThrowsInvalidOperationException()
        {
            // Arrange
            var request = new CreateConnectionRequest
            {
                WorkspaceId = Guid.NewGuid(),
                ClientSecret = "secret"
            };

            var connection = new Entities.Connection
            {
                WorkspaceId = request.WorkspaceId,
                ClientSecret = request.ClientSecret
            };

            _connectionMapper
                .Setup(x => x.Map(request))
                .Returns(connection);

            _unitOfWork
                .Setup(x => x.ConnectionRepository
                    .CheckDuplicateWorkspaceIdAsync(
                        request.WorkspaceId,
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync((Entities.Connection?)null);

            _unitOfWork
                .Setup(x => x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateConnectionAsync(request));
        }
    }
}
