using Azure;
using FabricExplorerBackend.Features.Warehouse.Contracts;
using FabricExplorerBackend.Infrastructures.Fabric.Context;
using FabricExplorerBackend.UnitTests.Common;
using FabricExplorerBackend.UnitTests.Features.Warehouse.Abstraction;
using Moq;
using Entities = FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.UnitTests.Features.Warehouse
{
    public class GetAllWarehouses_Tests : WarehouseServiceTestBase
    {
        [Fact]
        public async Task GetAllWarehousesAsync_WhenConnectionNotFound_ReturnsNotFound()
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
            var result = await _service.GetAllWarehousesAsync(connectionId, _workspaceId);

            // Assert
            Assert.False(result.IsSuccess);
            _fabricContextFactory.Verify(x =>
                x.CreateFabricContextWithoutTokenAsync(It.IsAny<Entities.Connection>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetAllWarehousesAsync_WhenContextIsNull_ThrowsInvalidOperationException()
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
                _service.GetAllWarehousesAsync(_connectionId, _workspaceId));
        }

        [Fact]
        public async Task GetAllWarehousesAsync_WhenValidRequest_ReturnsMappedWarehouseList()
        {
            // Arrange
            var connection = new Entities.Connection { Id = _connectionId };

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    _connectionId,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(connection);

            var (fabricClient, mockWorkspaces, mockWarehouseItems, _, _, _, _) = FabricTestHelper.CreateMockFabricClientHierarchy();

            var workspaceInfo = FabricTestHelper.CreateWorkspaceInfo(_workspaceId, "Analytics Workspace");
            mockWorkspaces
                .Setup(x => x.GetWorkspaceAsync(_workspaceId, It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(workspaceInfo, Mock.Of<Response>()));

            var wh1Id = Guid.NewGuid();
            var wh2Id = Guid.NewGuid();
            var wh1 = FabricTestHelper.CreateWarehouse(wh1Id, "Warehouse 1");
            var wh2 = FabricTestHelper.CreateWarehouse(wh2Id, "Warehouse 2");

            var asyncPageable = FabricTestHelper.CreateAsyncPageable(new[] { wh1, wh2 });

            mockWarehouseItems
                .Setup(x => x.ListWarehousesAsync(_workspaceId, It.IsAny<bool?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
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
            var result = await _service.GetAllWarehousesAsync(_connectionId, _workspaceId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);

            var list = result.Data.ToList();
            Assert.Equal(2, list.Count);
            Assert.Equal("Warehouse 1", list[0].WarehouseName);
            Assert.Equal("Analytics Workspace", list[0].WorkspaceName);
            Assert.Equal(wh1Id, list[0].WarehouseId);
            Assert.Equal(_workspaceId, list[0].WorkspaceId);
            Assert.Equal(_connectionId, list[0].ConnectionId);

            Assert.Equal("Warehouse 2", list[1].WarehouseName);
            Assert.Equal(wh2Id, list[1].WarehouseId);
        }

        [Fact]
        public async Task GetAllWarehousesAsync_WhenBypassCacheIsTrue_PassesBypassFlag()
        {
            // Arrange
            var connection = new Entities.Connection { Id = _connectionId };

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    _connectionId,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(connection);

            var (fabricClient, mockWorkspaces, mockWarehouseItems, _, _, _, _) = FabricTestHelper.CreateMockFabricClientHierarchy();
            var workspaceInfo = FabricTestHelper.CreateWorkspaceInfo(_workspaceId, "Analytics Workspace");
            mockWorkspaces
                .Setup(x => x.GetWorkspaceAsync(_workspaceId, It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(workspaceInfo, Mock.Of<Response>()));

            var wh = FabricTestHelper.CreateWarehouse(_warehouseId, "Warehouse 1");
            var asyncPageable = FabricTestHelper.CreateAsyncPageable(new[] { wh });

            mockWarehouseItems
                .Setup(x => x.ListWarehousesAsync(_workspaceId, It.IsAny<bool?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
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
            var result = await _service.GetAllWarehousesAsync(_connectionId, _workspaceId, bypassCache: true);

            // Assert
            Assert.True(result.IsSuccess);
            _cachedFetcher.Verify(x =>
                x.GetOrFetchAsync(
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<Func<CancellationToken, Task<IReadOnlyList<WarehouseListItemResponse>>>>(),
                    true,
                    It.IsAny<Func<IReadOnlyList<WarehouseListItemResponse>, bool>?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
