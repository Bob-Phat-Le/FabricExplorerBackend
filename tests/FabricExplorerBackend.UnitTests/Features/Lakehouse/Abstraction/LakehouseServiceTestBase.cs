using FabricExplorerBackend.Application.EventStreaming;
using FabricExplorerBackend.Application.Lakehouse;
using FabricExplorerBackend.Application.Lakehouse.Contracts;
using FabricExplorerBackend.Application.Operation;
using FabricExplorerBackend.Features.Fabric.Lakehouse;
using FabricExplorerBackend.Infrastructures.Cache;
using FabricExplorerBackend.Infrastructures.Fabric.Context;
using FabricExplorerBackend.Infrastructures.Fabric.FabricRestClient;
using FabricExplorerBackend.Infrastructures.Options;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Entities = FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.UnitTests.Features.Lakehouse.Abstraction
{
    public abstract class LakehouseServiceTestBase
    {
        protected readonly Mock<ILogger<LakehouseService>> _logger = new();
        protected readonly Mock<IUnitOfWork> _unitOfWork = new();
        protected readonly Mock<IFabricRestClientFactory> _fabricRestClientFactory = new();
        protected readonly Mock<IFabricRestClient> _fabricRestClient = new();
        protected readonly Mock<IFabricContextFactory> _fabricContextFactory = new();
        protected readonly Mock<IFabricOperationMapper> _fabricOperationMapper = new();
        protected readonly Mock<IFabricOperationService> _fabricOperationService = new();
        protected readonly Mock<ILakehouseMapper> _lakehouseMapper = new();
        protected readonly Mock<ILakehouseTableStatsService> _tableStatsService = new();
        protected readonly Mock<IEventStreamService> _eventStreamService = new();
        protected readonly Mock<ICachedFetcher> _cachedFetcher = new();

        protected readonly Guid _connectionId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        protected readonly Guid _workspaceId = Guid.Parse("12345678-abcd-1234-abcd-1234567890ab");
        protected readonly Guid _lakehouseId = Guid.Parse("77777777-7777-7777-7777-777777777777");

        protected readonly ILakehouseService _service;

        protected LakehouseServiceTestBase()
        {
            var fabricOptions = Options.Create(new FabricOptions
            {
                Api = new FabricApiOptions { BaseUrl = "https://api.fabric.microsoft.com/v1" }
            });

            var cacheOptions = Options.Create(new CacheOptions
            {
                ItemList = TimeSpan.FromMinutes(5)
            });

            _cachedFetcher
                .Setup(x => x.CreateKey(It.IsAny<string[]>()))
                .Returns((string[] parts) => string.Join(":", parts));

            _cachedFetcher
                .Setup(x => x.GetOrFetchAsync(
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<Func<CancellationToken, Task<IReadOnlyList<LakehouseListItemResponse>>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Func<IReadOnlyList<LakehouseListItemResponse>, bool>?>(),
                    It.IsAny<CancellationToken>()))
                .Returns((string key, TimeSpan ttl, Func<CancellationToken, Task<IReadOnlyList<LakehouseListItemResponse>>> fetch, bool bypass, Func<IReadOnlyList<LakehouseListItemResponse>, bool>? cacheWhen, CancellationToken ct) => fetch(ct));

            _fabricRestClientFactory
                .Setup(x => x.CreateFabricRestClient(It.IsAny<Entities.Connection>()))
                .Returns(_fabricRestClient.Object);

            _service = new LakehouseService(
                _logger.Object,
                _unitOfWork.Object,
                fabricOptions,
                cacheOptions,
                _fabricRestClientFactory.Object,
                _fabricContextFactory.Object,
                _fabricOperationMapper.Object,
                _fabricOperationService.Object,
                _lakehouseMapper.Object,
                _tableStatsService.Object,
                _eventStreamService.Object,
                _cachedFetcher.Object);
        }
    }
}
