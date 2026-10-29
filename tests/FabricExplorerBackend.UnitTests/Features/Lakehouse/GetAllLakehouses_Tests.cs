using Azure;
using FabricExplorerBackend.Features.Lakehouse.Contracts;
using FabricExplorerBackend.Infrastructures.Fabric.Context;
using FabricExplorerBackend.UnitTests.Common;
using FabricExplorerBackend.UnitTests.Features.Lakehouse.Abstraction;
using Moq;
using Entities = FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.UnitTests.Features.Lakehouse
{
    public class GetAllLakehouses_Tests : LakehouseServiceTestBase
    {
        [Fact]
        public async Task GetAllLakehousesAsync_WhenConnectionNotFound_ReturnsNotFound()
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
            var result = await _service.GetAllLakehousesAsync(connectionId, _workspaceId);

            // Assert
            Assert.False(result.IsSuccess);
            _fabricContextFactory.Verify(x =>
                x.CreateFabricContextWithoutTokenAsync(It.IsAny<Entities.Connection>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetAllLakehousesAsync_WhenContextIsNull_ThrowsInvalidOperationException()
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
                _service.GetAllLakehousesAsync(_connectionId, _workspaceId));
        }

        [Fact]
        public async Task GetAllLakehousesAsync_WhenValidRequest_ReturnsSortedList()
        {
            // Arrange
            var connection = new Entities.Connection { Id = _connectionId };

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    _connectionId,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(connection);

            var (fabricClient, mockWorkspaces, _, mockLakehouseItems, _, _, _) = FabricTestHelper.CreateMockFabricClientHierarchy();

            var workspaceInfo = FabricTestHelper.CreateWorkspaceInfo(_workspaceId, "Lakehouse Workspace");
            mockWorkspaces
                .Setup(x => x.GetWorkspaceAsync(_workspaceId, It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(workspaceInfo, Mock.Of<Response>()));

            var lhBetaId = Guid.NewGuid();
            var lhAlphaId = Guid.NewGuid();
            var lhBeta = FabricTestHelper.CreateLakehouse(lhBetaId, "BetaLakehouse");
            var lhAlpha = FabricTestHelper.CreateLakehouse(lhAlphaId, "AlphaLakehouse");

            var asyncPageable = FabricTestHelper.CreateAsyncPageable(new[] { lhBeta, lhAlpha });

            mockLakehouseItems
                .Setup(x => x.ListLakehousesAsync(_workspaceId, It.IsAny<bool?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
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
            var result = await _service.GetAllLakehousesAsync(_connectionId, _workspaceId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);

            var list = result.Data.ToList();
            Assert.Equal(2, list.Count);
            // Verify sorted alphabetically: AlphaLakehouse before BetaLakehouse
            Assert.Equal("AlphaLakehouse", list[0].LakehouseName);
            Assert.Equal(lhAlphaId, list[0].LakehouseId);
            Assert.Equal("BetaLakehouse", list[1].LakehouseName);
            Assert.Equal(lhBetaId, list[1].LakehouseId);
            Assert.Equal("Lakehouse Workspace", list[0].WorkspaceName);
        }

        [Fact]
        public async Task GetAllLakehousesAsync_WhenBypassCacheIsTrue_PassesBypassFlag()
        {
            // Arrange
            var connection = new Entities.Connection { Id = _connectionId };

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    _connectionId,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(connection);

            var (fabricClient, mockWorkspaces, _, mockLakehouseItems, _, _, _) = FabricTestHelper.CreateMockFabricClientHierarchy();
            var workspaceInfo = FabricTestHelper.CreateWorkspaceInfo(_workspaceId, "Lakehouse Workspace");
            mockWorkspaces
                .Setup(x => x.GetWorkspaceAsync(_workspaceId, It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(workspaceInfo, Mock.Of<Response>()));

            var lh = FabricTestHelper.CreateLakehouse(_lakehouseId, "MyLakehouse");
            var asyncPageable = FabricTestHelper.CreateAsyncPageable(new[] { lh });

            mockLakehouseItems
                .Setup(x => x.ListLakehousesAsync(_workspaceId, It.IsAny<bool?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
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
            var result = await _service.GetAllLakehousesAsync(_connectionId, _workspaceId, bypassCache: true);

            // Assert
            Assert.True(result.IsSuccess);
            _cachedFetcher.Verify(x =>
                x.GetOrFetchAsync(
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<Func<CancellationToken, Task<IReadOnlyList<LakehouseListItemResponse>>>>(),
                    true,
                    It.IsAny<Func<IReadOnlyList<LakehouseListItemResponse>, bool>?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
