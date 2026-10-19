using FabricExplorerBackend.Application.MirroredDatabase;
using FabricExplorerBackend.Application.MirroredDatabase.Contracts;
using FabricExplorerBackend.Application.Operation;
using FabricExplorerBackend.Infrastructures.Cache;
using FabricExplorerBackend.Infrastructures.Fabric.Context;
using FabricExplorerBackend.Infrastructures.Fabric.FabricRestClient;
using FabricExplorerBackend.Infrastructures.Options;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using FabricExplorerBackend.UnitTests.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Fabric.Api;
using Microsoft.Fabric.Api.Core;
using Microsoft.Fabric.Api.MirroredDatabase;
using Moq;
using Entities = FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.UnitTests.Features.MirroredDatabase.Abstraction
{
    public abstract class MirroredDatabaseServiceTestBase
    {
        protected readonly Mock<IMirroredDatabaseMapper> _mapper = new();
        protected readonly Mock<IFabricContextFactory> _fabricContextFactory = new();
        protected readonly Mock<IFabricRestClientFactory> _fabricRestClientFactory = new();
        protected readonly Mock<IFabricRestClient> _fabricRestClient = new();
        protected readonly Mock<IFabricOperationMapper> _fabricOperationMapper = new();
        protected readonly Mock<IFabricOperationService> _fabricOperationService = new();
        protected readonly Mock<ICachedFetcher> _cachedFetcher = new();
        protected readonly Mock<IUnitOfWork> _unitOfWork = new();
        protected readonly Mock<IConnectionRepository> _connectionRepository = new();
        protected readonly Mock<ILogger<MirroredDatabaseService>> _logger = new();

        protected readonly FabricClient _fabricClient;
        protected readonly Mock<WorkspacesClient> _mockWorkspaces;
        protected readonly Mock<Microsoft.Fabric.Api.MirroredDatabase.ItemsClient> _mockMirroredDbItems;
        protected readonly Mock<MirroringClient> _mockMirroredDbMirroring;
        protected readonly FabricContext _fabricContext;

        protected readonly Guid _connectionId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        protected readonly Guid _workspaceId = Guid.Parse("66666666-7777-8888-9999-000000000000");
        protected readonly Guid _mirroredDatabaseId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        protected readonly IMirroredDatabaseService _service;

        protected MirroredDatabaseServiceTestBase()
        {
            var (client, workspaces, _, _, _, mirroredDbItems, mirroredDbMirroring) =
                FabricTestHelper.CreateMockFabricClientHierarchy();

            _fabricClient = client;
            _mockWorkspaces = workspaces;
            _mockMirroredDbItems = mirroredDbItems;
            _mockMirroredDbMirroring = mirroredDbMirroring;
            _fabricContext = new FabricContext
            {
                ConnectionId = _connectionId,
                Client = _fabricClient
            };

            _unitOfWork
                .Setup(u => u.ConnectionRepository)
                .Returns(_connectionRepository.Object);

            var cacheOptions = Options.Create(new CacheOptions
            {
                ItemList = TimeSpan.FromMinutes(5),
                MirroringDefinition = TimeSpan.FromMinutes(10)
            });

            var fabricOptions = Options.Create(new FabricOptions
            {
                Api = new FabricApiOptions { BaseUrl = "https://api.fabric.microsoft.com/v1" },
                SqlSchemas = new SqlSchemas { Default = "dbo" }
            });

            _cachedFetcher
                .Setup(x => x.CreateKey(It.IsAny<string[]>()))
                .Returns((string[] parts) => string.Join(":", parts));

            // Default pass-through for cachedFetcher.GetOrFetchAsync
            _cachedFetcher
                .Setup(x => x.GetOrFetchAsync(
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<Func<CancellationToken, Task<IReadOnlyList<MirroredDatabaseListItemResponse>>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Func<IReadOnlyList<MirroredDatabaseListItemResponse>, bool>?>(),
                    It.IsAny<CancellationToken>()))
                .Returns((string key, TimeSpan ttl, Func<CancellationToken, Task<IReadOnlyList<MirroredDatabaseListItemResponse>>> fetch, bool bypass, Func<IReadOnlyList<MirroredDatabaseListItemResponse>, bool>? cacheWhen, CancellationToken ct) => fetch(ct));

            _cachedFetcher
                .Setup(x => x.GetOrFetchAsync(
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<Func<CancellationToken, Task<MirroringDefinitionInfo?>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Func<MirroringDefinitionInfo?, bool>?>(),
                    It.IsAny<CancellationToken>()))
                .Returns((string key, TimeSpan ttl, Func<CancellationToken, Task<MirroringDefinitionInfo?>> fetch, bool bypass, Func<MirroringDefinitionInfo?, bool>? cacheWhen, CancellationToken ct) => fetch(ct));

            _fabricRestClientFactory
                .Setup(f => f.CreateFabricRestClient(It.IsAny<Entities.Connection>()))
                .Returns(_fabricRestClient.Object);

            _service = new MirroredDatabaseService(
                _mapper.Object,
                _fabricContextFactory.Object,
                _fabricRestClientFactory.Object,
                _fabricOperationMapper.Object,
                _fabricOperationService.Object,
                _cachedFetcher.Object,
                cacheOptions,
                fabricOptions,
                _unitOfWork.Object,
                _logger.Object);
        }

        protected Entities.Connection SetupConnection(Guid? id = null)
        {
            var targetId = id ?? _connectionId;
            var connection = new Entities.Connection
            {
                Id = targetId
            };

            _connectionRepository
                .Setup(r => r.GetByIdAsync(targetId, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(connection);

            _fabricContextFactory
                .Setup(f => f.CreateFabricContextWithoutTokenAsync(connection, It.IsAny<CancellationToken>()))
                .ReturnsAsync(_fabricContext);

            return connection;
        }
    }
}
