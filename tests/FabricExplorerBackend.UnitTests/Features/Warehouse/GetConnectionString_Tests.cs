using Azure;
using FabricExplorerBackend.Infrastructures.Fabric.Context;
using FabricExplorerBackend.UnitTests.Common;
using FabricExplorerBackend.UnitTests.Features.Warehouse.Abstraction;
using Microsoft.Fabric.Api.Warehouse.Models;
using Moq;
using Entities = FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.UnitTests.Features.Warehouse
{
    public class GetConnectionString_Tests : WarehouseServiceTestBase
    {
        [Fact]
        public async Task GetConnectionStringAsync_WhenConnectionNotFound_ReturnsNotFound()
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
            var result = await _service.GetConnectionStringAsync(connectionId, _workspaceId, _warehouseId);

            // Assert
            Assert.False(result.IsSuccess);
            _fabricContextFactory.Verify(x =>
                x.CreateFabricContextWithoutTokenAsync(It.IsAny<Entities.Connection>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetConnectionStringAsync_WhenValidWarehouse_ReturnsConnectionString()
        {
            // Arrange
            var connection = new Entities.Connection { Id = _connectionId };

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    _connectionId,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(connection);

            var (fabricClient, _, mockWarehouseItems, _, _, _, _) = FabricTestHelper.CreateMockFabricClientHierarchy();

            var expectedConnStr = "Server=tcp:fabric.datawarehouse.pbidedicated.windows.net,1433;Database=MyWh";
            var connResult = FabricTestHelper.CreateConnectionStringResult(expectedConnStr);

            mockWarehouseItems
                .Setup(x => x.GetConnectionStringAsync(
                    _workspaceId,
                    _warehouseId,
                    It.IsAny<Guid?>(),
                    It.IsAny<PrivateLinkType?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(connResult, Mock.Of<Response>()));

            var context = new FabricContext
            {
                ConnectionId = _connectionId,
                Client = fabricClient
            };

            _fabricContextFactory
                .Setup(x => x.CreateFabricContextWithoutTokenAsync(connection, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context);

            // Act
            var result = await _service.GetConnectionStringAsync(_connectionId, _workspaceId, _warehouseId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(_warehouseId, result.Data.WarehouseId);
            Assert.Equal(expectedConnStr, result.Data.ConnectionString);
        }
    }
}
