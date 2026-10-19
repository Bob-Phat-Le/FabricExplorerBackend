using Azure;
using FabricExplorerBackend.Application.Warhouse;
using FabricExplorerBackend.Infrastructures.Fabric.Context;
using FabricExplorerBackend.Infrastructures.Sql;
using FabricExplorerBackend.UnitTests.Common;
using FabricExplorerBackend.UnitTests.Features.Warehouse.Abstraction;
using Moq;
using Entities = FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.UnitTests.Features.Warehouse
{
    public class GetWarehouseById_Tests : WarehouseServiceTestBase
    {
        [Fact]
        public async Task GetWarehouseByIdAsync_WhenConnectionNotFound_ReturnsNotFound()
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
            var result = await _service.GetWarehouseByIdAsync(connectionId, _workspaceId, _warehouseId);

            // Assert
            Assert.False(result.IsSuccess);
            _fabricContextFactory.Verify(x =>
                x.CreateFabricContextWithoutTokenAsync(It.IsAny<Entities.Connection>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetWarehouseByIdAsync_WhenWarehouseHasConnectionString_ProbesSqlAndReturnsOnline()
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

            var capacityId = Guid.NewGuid();
            var workspaceInfo = FabricTestHelper.CreateWorkspaceInfo(_workspaceId, "My Analytics Workspace");
            FabricTestHelper.SetField(workspaceInfo, "CapacityId", (Guid?)capacityId);

            var createdDate = DateTimeOffset.UtcNow.AddDays(-10);
            var updatedDate = DateTimeOffset.UtcNow.AddDays(-1);
            var warehouse = FabricTestHelper.CreateWarehouse(
                _warehouseId,
                "GoldWarehouse",
                "Analytics gold layer",
                "gold-server.fabric.microsoft.com",
                createdDate,
                updatedDate);

            mockWorkspaces
                .Setup(x => x.GetWorkspaceAsync(_workspaceId, It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(workspaceInfo, Mock.Of<Response>()));

            mockWarehouseItems
                .Setup(x => x.GetWarehouseAsync(_workspaceId, _warehouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(warehouse, Mock.Of<Response>()));

            _sqlQueryService
                .Setup(x => x.ExecuteScalarAsync<int?>(
                    connection,
                    It.Is<SqlEndpoint>(ep => ep.Server == "gold-server.fabric.microsoft.com" && ep.Database == "GoldWarehouse"),
                    "SELECT 1",
                    It.IsAny<IReadOnlyDictionary<string, object?>?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var context = new FabricContext
            {
                ConnectionId = _connectionId,
                Client = fabricClient
            };

            _fabricContextFactory
                .Setup(x => x.CreateFabricContextWithoutTokenAsync(connection, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context);

            // Act
            var result = await _service.GetWarehouseByIdAsync(_connectionId, _workspaceId, _warehouseId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);

            Assert.Equal(_warehouseId, result.Data.Id);
            Assert.Equal("GoldWarehouse", result.Data.Name);
            Assert.Equal("Analytics gold layer", result.Data.Description);
            Assert.Equal(WarehouseOnlineStatus.Online.ToString(), result.Data.OnlineStatus);
            Assert.Null(result.Data.OnlineStatusMessage);
            Assert.Equal(_workspaceId, result.Data.Workspace!.Id);
            Assert.Equal("My Analytics Workspace", result.Data.Workspace.Name);
            Assert.Equal(capacityId, result.Data.Workspace.CapacityId);

            _sqlQueryService.Verify(x =>
                x.ExecuteScalarAsync<int?>(
                    connection,
                    It.IsAny<SqlEndpoint>(),
                    "SELECT 1",
                    It.IsAny<IReadOnlyDictionary<string, object?>?>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetWarehouseByIdAsync_WhenWarehouseHasNoConnectionString_ReturnsUnknownStatus()
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

            var workspaceInfo = FabricTestHelper.CreateWorkspaceInfo(_workspaceId, "Workspace");
            var warehouse = FabricTestHelper.CreateWarehouse(
                _warehouseId,
                "ProvisioningWarehouse",
                connectionString: null);

            mockWorkspaces
                .Setup(x => x.GetWorkspaceAsync(_workspaceId, It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(workspaceInfo, Mock.Of<Response>()));

            mockWarehouseItems
                .Setup(x => x.GetWarehouseAsync(_workspaceId, _warehouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(warehouse, Mock.Of<Response>()));

            var context = new FabricContext
            {
                ConnectionId = _connectionId,
                Client = fabricClient
            };

            _fabricContextFactory
                .Setup(x => x.CreateFabricContextWithoutTokenAsync(connection, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context);

            // Act
            var result = await _service.GetWarehouseByIdAsync(_connectionId, _workspaceId, _warehouseId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(WarehouseOnlineStatus.Unknown.ToString(), result.Data.OnlineStatus);
            Assert.Contains("connection string", result.Data.OnlineStatusMessage);

            _sqlQueryService.Verify(x =>
                x.ExecuteScalarAsync<int?>(
                    It.IsAny<Entities.Connection>(),
                    It.IsAny<SqlEndpoint>(),
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyDictionary<string, object?>?>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
