using FabricExplorerBackend.Features.Workspace.Contracts;
using FabricExplorerBackend.Infrastructures.Fabric.Context;
using FabricExplorerBackend.UnitTests.Common;
using FabricExplorerBackend.UnitTests.Features.Workspace.Abstraction;
using Moq;
using Entities = FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.UnitTests.Features.Workspace
{
    public class GetAllWorkspaces_Tests : WorkspaceServiceTestBase
    {
        [Fact]
        public async Task GetAllWorkspacesAsync_WhenConnectionNotFound_ReturnsNotFound()
        {
            // Arrange
            var connectionId = Guid.NewGuid();
            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    connectionId,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Entities.Connection?)null);

            // Act
            var result = await _service.GetAllWorkspacesAsync(connectionId);

            // Assert
            Assert.False(result.IsSuccess);
            _fabricContextFactory.Verify(x =>
                x.CreateFabricContextWithoutTokenAsync(It.IsAny<Entities.Connection>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetAllWorkspacesAsync_WhenContextIsNull_ThrowsInvalidOperationException()
        {
            // Arrange
            var connection = new Entities.Connection { Id = _connectionId };

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    _connectionId,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(connection);

            _fabricContextFactory
                .Setup(x => x.CreateFabricContextWithoutTokenAsync(connection, It.IsAny<CancellationToken>()))
                .ReturnsAsync((FabricContext?)null);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GetAllWorkspacesAsync(_connectionId));
        }

        [Fact]
        public async Task GetAllWorkspacesAsync_WhenValidRequest_ReturnsAlphabeticallySortedList()
        {
            // Arrange
            var connection = new Entities.Connection { Id = _connectionId };

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    _connectionId,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(connection);

            var (fabricClient, mockWorkspaces, _, _, _, _, _) = FabricTestHelper.CreateMockFabricClientHierarchy();

            var capId = Guid.NewGuid();
            var wsBeta = FabricTestHelper.CreateWorkspace(_workspaceId2, "Beta Workspace", capId.ToString());
            var wsAlpha = FabricTestHelper.CreateWorkspace(_workspaceId1, "Alpha Workspace", capId.ToString());

            var asyncPageable = FabricTestHelper.CreateAsyncPageable(new[] { wsBeta, wsAlpha });

            mockWorkspaces
                .Setup(x => x.ListWorkspacesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                .Returns(asyncPageable);

            var context = new FabricContext
            {
                ConnectionId = _connectionId,
                Client = fabricClient
            };

            _fabricContextFactory
                .Setup(x => x.CreateFabricContextWithoutTokenAsync(connection, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context);

            // Act
            var result = await _service.GetAllWorkspacesAsync(_connectionId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);

            var list = result.Data.ToList();
            Assert.Equal(2, list.Count);
            // Verify sorted alphabetically: Alpha before Beta
            Assert.Equal("Alpha Workspace", list[0].WorkspaceName);
            Assert.Equal(_workspaceId1, list[0].WorkspaceId);
            Assert.Equal("Beta Workspace", list[1].WorkspaceName);
            Assert.Equal(_workspaceId2, list[1].WorkspaceId);
        }

        [Fact]
        public async Task GetAllWorkspacesAsync_WhenBypassCacheIsTrue_PassesBypassFlag()
        {
            // Arrange
            var connection = new Entities.Connection { Id = _connectionId };

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    _connectionId,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(connection);

            var (fabricClient, mockWorkspaces, _, _, _, _, _) = FabricTestHelper.CreateMockFabricClientHierarchy();
            var ws = FabricTestHelper.CreateWorkspace(_workspaceId1, "My Workspace");
            var asyncPageable = FabricTestHelper.CreateAsyncPageable(new[] { ws });

            mockWorkspaces
                .Setup(x => x.ListWorkspacesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                .Returns(asyncPageable);

            var context = new FabricContext
            {
                ConnectionId = _connectionId,
                Client = fabricClient
            };

            _fabricContextFactory
                .Setup(x => x.CreateFabricContextWithoutTokenAsync(connection, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context);

            // Act
            var result = await _service.GetAllWorkspacesAsync(_connectionId, bypassCache: true);

            // Assert
            Assert.True(result.IsSuccess);
            _cachedFetcher.Verify(x =>
                x.GetOrFetchAsync(
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<Func<CancellationToken, Task<IReadOnlyList<WorkspaceListItemResponse>>>>(),
                    true,
                    It.IsAny<Func<IReadOnlyList<WorkspaceListItemResponse>, bool>?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
