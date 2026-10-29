using FabricExplorerBackend.Application.Lakehouse.Contracts;
using FabricExplorerBackend.Commons.Paginations;
using FabricExplorerBackend.Features.Lakehouse;
using FabricExplorerBackend.Infrastructures.Fabric.Dtos;
using FabricExplorerBackend.UnitTests.Features.Lakehouse.Abstraction;
using Moq;
using Entities = FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.UnitTests.Features.Lakehouse
{
    public class GetAllTables_Tests : LakehouseServiceTestBase
    {
        [Fact]
        public async Task GetAllTablesAsync_WhenConnectionNotFound_ReturnsNotFound()
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
            var result = await _service.GetAllTablesAsync(connectionId, _workspaceId, _lakehouseId, null);

            // Assert
            Assert.False(result.IsSuccess);
            _fabricRestClient.Verify(x =>
                x.GetAsync<FabricValueListResponse<FabricTableResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetAllTablesAsync_FirstPage_FetchesAndEnrichesStats()
        {
            // Arrange
            var connection = new Entities.Connection { Id = _connectionId };

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    _connectionId,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(connection);

            var pagination = new PaginationWithCursorRequest
            {
                PageSize = 10,
                ContinuationToken = null
            };

            var table1 = new FabricTableResponse { Name = "Customer", Format = "delta", Type = "Managed" };
            var table2 = new FabricTableResponse { Name = "Orders", Format = "delta", Type = "Managed" };
            var fabricResponse = new FabricValueListResponse<FabricTableResponse>
            {
                Data = new List<FabricTableResponse?> { table1, table2 },
                ContinuationToken = "next-token-123"
            };

            var expectedUrl = $"https://api.fabric.microsoft.com/v1/workspaces/{_workspaceId}/lakehouses/{_lakehouseId}/tables?pageSize=10";

            _fabricRestClient
                .Setup(x => x.GetAsync<FabricValueListResponse<FabricTableResponse>>(expectedUrl, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fabricResponse);

            var now = DateTimeOffset.UtcNow;
            var statsDictionary = new Dictionary<string, TableStats>
            {
                ["Customer"] = new TableStats(1500, now, TableStatus.Healthy, null),
                ["Orders"] = new TableStats(50000, now, TableStatus.Healthy, null)
            };

            _tableStatsService
                .Setup(x => x.GetStatsAsync(
                    connection, _workspaceId, _lakehouseId, It.IsAny<IEnumerable<FabricTableResponse>>(), false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(statsDictionary);

            _lakehouseMapper
                .Setup(x => x.Map(table1))
                .Returns(new TableResponse { Name = "Customer", Format = "delta", Type = "Managed" });

            _lakehouseMapper
                .Setup(x => x.Map(table2))
                .Returns(new TableResponse { Name = "Orders", Format = "delta", Type = "Managed" });

            // Act
            var result = await _service.GetAllTablesAsync(_connectionId, _workspaceId, _lakehouseId, pagination);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.NotNull(result.Data.InnerData);

            var items = result.Data.InnerData.Items.ToList();
            Assert.Equal(2, items.Count);

            Assert.Equal("Customer", items[0].Name);
            Assert.Equal(1500, items[0].RowCount);
            Assert.Equal(now, items[0].LastModifiedTime);
            Assert.Equal(TableStatus.Healthy.ToString(), items[0].Status);

            Assert.Equal("Orders", items[1].Name);
            Assert.Equal(50000, items[1].RowCount);

            Assert.Equal(10, result.Data.InnerData.PageInformation!.PageSize);
            Assert.Equal("next-token-123", result.Data.InnerData.PageInformation.NextToken);
            Assert.Equal(2, result.Data.InnerData.PageInformation.ItemsCount);
        }

        [Fact]
        public async Task GetAllTablesAsync_WithContinuationToken_AppendsTokenToUrl()
        {
            // Arrange
            var connection = new Entities.Connection { Id = _connectionId };

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    _connectionId,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(connection);

            var pagination = new PaginationWithCursorRequest
            {
                PageSize = 25,
                ContinuationToken = "token ABC"
            };

            var fabricResponse = new FabricValueListResponse<FabricTableResponse>
            {
                Data = new List<FabricTableResponse?>(),
                ContinuationToken = string.Empty
            };

            var expectedUrl = $"https://api.fabric.microsoft.com/v1/workspaces/{_workspaceId}/lakehouses/{_lakehouseId}/tables?pageSize=25&continuationToken=token%20ABC";

            _fabricRestClient
                .Setup(x => x.GetAsync<FabricValueListResponse<FabricTableResponse>>(expectedUrl, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fabricResponse);

            _tableStatsService
                .Setup(x => x.GetStatsAsync(
                    connection, _workspaceId, _lakehouseId, It.IsAny<IEnumerable<FabricTableResponse>>(), false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, TableStats>());

            // Act
            var result = await _service.GetAllTablesAsync(_connectionId, _workspaceId, _lakehouseId, pagination);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.NotNull(result.Data.InnerData);
            Assert.Empty(result.Data.InnerData.Items);

            _fabricRestClient.Verify(x =>
                x.GetAsync<FabricValueListResponse<FabricTableResponse>>(expectedUrl, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
