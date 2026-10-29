using FabricExplorerBackend.Features.Lakehouse;
using FabricExplorerBackend.Infrastructures.Cache;
using FabricExplorerBackend.Infrastructures.Fabric.Dtos;
using FabricExplorerBackend.Infrastructures.Fabric.FabricRestClient;
using FabricExplorerBackend.Infrastructures.Identity;
using FabricExplorerBackend.Infrastructures.Options;
using FabricExplorerBackend.Infrastructures.Sql;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Entities = FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.UnitTests.Features.Lakehouse
{
    public class LakehouseTableStatsService_Tests
    {
        private readonly Mock<IFabricRestClientFactory> _fabricRestClientFactory = new();
        private readonly Mock<ITokenService> _tokenService = new();
        private readonly Mock<ISqlQueryService> _sqlQueryService = new();
        private readonly Mock<ILogger<LakehouseTableStatsService>> _logger = new();
        private readonly Mock<ICachedFetcher> _cachedFetcher = new();

        private readonly ILakehouseTableStatsService _service;

        public LakehouseTableStatsService_Tests()
        {
            var fabricOptions = Options.Create(new FabricOptions
            {
                Api = new FabricApiOptions { BaseUrl = "https://api.fabric.microsoft.com/v1" },
                SqlSchemas = new SqlSchemas { Default = "dbo" }
            });

            var cacheOptions = Options.Create(new CacheOptions
            {
                TableStats = TimeSpan.FromMinutes(10),
                TableStale = TimeSpan.FromDays(7)
            });

            var operationPollingOptions = Options.Create(new OperationPollingOptions
            {
                TableStatsMaxParallelism = 4
            });

            var azureOptions = Options.Create(new AzureOptions
            {
                Scopes = new ScopeOptions { Storage = "https://storage.azure.com/.default" }
            });

            _cachedFetcher
                .Setup(x => x.CreateKey(It.IsAny<string[]>()))
                .Returns((string[] parts) => string.Join(":", parts));

            _service = new LakehouseTableStatsService(
                fabricOptions,
                cacheOptions,
                operationPollingOptions,
                azureOptions,
                _fabricRestClientFactory.Object,
                _tokenService.Object,
                _sqlQueryService.Object,
                _logger.Object,
                _cachedFetcher.Object);
        }

        [Fact]
        public async Task GetStatsAsync_WhenNoDeltaTables_ReturnsUnknownWithNotDeltaTableMessage()
        {
            // Arrange
            var connection = new Entities.Connection { Id = Guid.NewGuid() };
            var tables = new List<FabricTableResponse>
            {
                new() { Name = "CsvTable", Format = "csv" },
                new() { Name = "ParquetTable", Format = "parquet" }
            };

            // Act
            var stats = await _service.GetStatsAsync(connection, Guid.NewGuid(), Guid.NewGuid(), tables);

            // Assert
            Assert.Equal(2, stats.Count);
            Assert.Equal(TableStatus.Unknown, stats["CsvTable"].Status);
            Assert.Equal("Not delta table", stats["CsvTable"].StatusMessage);
            Assert.Equal(TableStatus.Unknown, stats["ParquetTable"].Status);
            Assert.Equal("Not delta table", stats["ParquetTable"].StatusMessage);

            _cachedFetcher.Verify(x =>
                x.GetOrFetchAsync(
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<Func<CancellationToken, Task<TableStats>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Func<TableStats, bool>?>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetStatsAsync_WhenDeltaTableCached_ReturnsCachedTableStats()
        {
            // Arrange
            var connection = new Entities.Connection { Id = Guid.NewGuid() };
            var tables = new List<FabricTableResponse>
            {
                new() { Name = "Customers", Format = "delta" }
            };

            var cachedStats = new TableStats(1000, DateTimeOffset.UtcNow, TableStatus.Healthy, null);

            _cachedFetcher
                .Setup(x => x.GetOrFetchAsync(
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<Func<CancellationToken, Task<TableStats>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Func<TableStats, bool>?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedStats);

            // Act
            var stats = await _service.GetStatsAsync(connection, Guid.NewGuid(), Guid.NewGuid(), tables);

            // Assert
            Assert.Single(stats);
            Assert.True(stats.ContainsKey("Customers"));
            Assert.Equal(1000, stats["Customers"].RowCount);
            Assert.Equal(TableStatus.Healthy, stats["Customers"].Status);
        }
    }
}
