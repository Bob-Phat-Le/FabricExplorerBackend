using FabricExplorerBackend.Application.Workspace;
using FabricExplorerBackend.Application.Workspace.Contracts;
using FabricExplorerBackend.Infrastructures.Cache;
using FabricExplorerBackend.Infrastructures.Fabric.Context;
using FabricExplorerBackend.Infrastructures.Options;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.Extensions.Options;
using Moq;

namespace FabricExplorerBackend.UnitTests.Features.Workspace.Abstraction
{
    public abstract class WorkspaceServiceTestBase
    {
        protected readonly Mock<IUnitOfWork> _unitOfWork = new();
        protected readonly Mock<IFabricContextFactory> _fabricContextFactory = new();
        protected readonly Mock<ICachedFetcher> _cachedFetcher = new();

        protected readonly Guid _connectionId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        protected readonly Guid _workspaceId1 = Guid.Parse("12345678-abcd-1234-abcd-1234567890ab");
        protected readonly Guid _workspaceId2 = Guid.Parse("87654321-abcd-4321-abcd-0987654321ba");

        protected readonly IWorkspaceService _service;

        protected WorkspaceServiceTestBase()
        {
            var cacheOptions = Options.Create(new CacheOptions
            {
                WorkspaceList = TimeSpan.FromMinutes(10)
            });

            _cachedFetcher
                .Setup(x => x.CreateKey(It.IsAny<string[]>()))
                .Returns((string[] parts) => string.Join(":", parts));

            _cachedFetcher
                .Setup(x => x.GetOrFetchAsync(
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<Func<CancellationToken, Task<IReadOnlyList<WorkspaceListItemResponse>>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<Func<IReadOnlyList<WorkspaceListItemResponse>, bool>?>(),
                    It.IsAny<CancellationToken>()))
                .Returns((string key, TimeSpan ttl, Func<CancellationToken, Task<IReadOnlyList<WorkspaceListItemResponse>>> fetch, bool bypass, Func<IReadOnlyList<WorkspaceListItemResponse>, bool>? cacheWhen, CancellationToken ct) => fetch(ct));

            _service = new WorkspaceService(
                _unitOfWork.Object,
                _fabricContextFactory.Object,
                _cachedFetcher.Object,
                cacheOptions);
        }
    }
}
