using FabricExplorerBackend.Application.EventStreaming;
using FabricExplorerBackend.Application.Warhouse;
using FabricExplorerBackend.Application.Warhouse.Contracts;
using FabricExplorerBackend.Infrastructures.Cache;
using FabricExplorerBackend.Infrastructures.Fabric.Context;
using FabricExplorerBackend.Infrastructures.Options;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using FabricExplorerBackend.Infrastructures.Sql;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FabricExplorerBackend.UnitTests.Features.Warehouse.Abstraction
{
    public abstract class WarehouseServiceTestBase
    {
        protected readonly Mock<IUnitOfWork> _unitOfWork = new();
        protected readonly Mock<IFabricContextFactory> _fabricContextFactory = new();
        protected readonly Mock<IEventStreamService> _eventStreamService = new();
        protected readonly Mock<ISqlQueryService> _sqlQueryService = new();
        protected readonly Mock<ICachedFetcher> _cachedFetcher = new();
        protected readonly Mock<ILogger<WarehouseService>> _logger = new();

        protected readonly Guid _connectionId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        protected readonly Guid _workspaceId = Guid.Parse("12345678-abcd-1234-abcd-1234567890ab");
        protected readonly Guid _warehouseId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        protected readonly IWarehouseService _service;

        protected WarehouseServiceTestBase()
        {
            var cacheOptions = Options.Create(new CacheOptions
            {
                ItemList = TimeSpan.FromMinutes(5),
                WarehouseProbeTimeout = TimeSpan.FromSeconds(5)
            });

            _cachedFetcher
                .Setup(x => x.CreateKey(It.IsAny<string[]>()))
                .Returns((string[] parts) => string.Join(":", parts));

            _cachedFetcher
                .Setup(x => x.GetOrFetchAsync(
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<Func<CancellationToken, Task<IReadOnlyList<WarehouseListItemResponse>>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Func<IReadOnlyList<WarehouseListItemResponse>, bool>?>(),
                    It.IsAny<CancellationToken>()))
                .Returns((string key, TimeSpan ttl, Func<CancellationToken, Task<IReadOnlyList<WarehouseListItemResponse>>> fetch, bool bypass, Func<IReadOnlyList<WarehouseListItemResponse>, bool>? cacheWhen, CancellationToken ct) => fetch(ct));

            _service = new WarehouseService(
                _unitOfWork.Object,
                _fabricContextFactory.Object,
                _eventStreamService.Object,
                cacheOptions,
                _cachedFetcher.Object,
                _logger.Object);
        }
    }
}
