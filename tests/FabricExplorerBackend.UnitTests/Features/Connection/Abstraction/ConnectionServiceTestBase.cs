using FabricExplorerBackend.Application.Connection;
using FabricExplorerBackend.Infrastructures.Cache;
using FabricExplorerBackend.Infrastructures.Fabric.Client;
using FabricExplorerBackend.Infrastructures.Fabric.Context;
using FabricExplorerBackend.Infrastructures.Identity;
using FabricExplorerBackend.Infrastructures.Options;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using FabricExplorerBackend.Infrastructures.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using Entities = FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.UnitTests.Features.Connection.Abstraction
{
    public abstract class ConnectionServiceTestBase
    {
        protected readonly Mock<ITokenService> _tokenService = new();
        protected readonly Mock<ICacheService> _cacheService = new();
        protected readonly Mock<IHttpContextAccessor> _httpContext = new();
        protected readonly Mock<IClientFactory> _clientFactory = new();
        protected readonly Mock<IFabricContextFactory> _contextFactory = new();
        protected readonly Mock<ISecretProtector> _secretProtector = new();
        protected readonly Mock<IUnitOfWork> _unitOfWork = new();
        protected readonly Mock<IConnectionMapper> _connectionMapper = new();

        protected Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        protected Guid _userPreferenceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        protected Guid _connectionId1 = Guid.Parse("33333333-3333-3333-3333-333333333333");
        protected Guid _connectionId2 = Guid.Parse("44444444-4444-4444-4444-444444444444");
        protected Guid _tenantId = Guid.Parse("72f988bf-86f1-41af-91ab-2d7cd011db47");
        protected Guid _clientId1 = Guid.Parse("8f3e1a2b-9c4d-4e5f-8a1b-3c5d7e9f2a4b");
        protected Guid _clientId2 = Guid.Parse("3a2b1c4d-8e7f-6a5b-4c3d-2e1f0a9b8c7d");
        protected Guid _workspaceId1 = Guid.Parse("12345678-abcd-1234-abcd-1234567890ab");
        protected Guid _workspaceId2 = Guid.Parse("87654321-abcd-4321-abcd-0987654321ba");
        protected DateTimeOffset _createdAt = new(2026, 9, 28, 0, 0, 0, TimeSpan.Zero);

        protected readonly IConnectionService _service;

        public ConnectionServiceTestBase()
        {
            var azureOptions = Options.Create(new AzureOptions
            {
                Scopes = new ScopeOptions
                {
                    Fabric = "https://api.fabric.microsoft.com/.default",
                    Storage = "https://storage.azure.com/.default",
                    Sql = "https://database.windows.net/.default"
                }
            });

            _service = new ConnectionService(
                azureOptions,
                _tokenService.Object,
                _cacheService.Object,
                _httpContext.Object,
                _clientFactory.Object,
                _contextFactory.Object,
                _secretProtector.Object,
                _unitOfWork.Object,
                _connectionMapper.Object);

            SetUpMockData();
            SetUpHttpContext();
        }

        protected void SetUpMockData()
        {
            var connection1 = new Entities.Connection
            {
                Id = _connectionId1,
                Name = "Production Fabric",
                CreatedAt = _createdAt,
                ClientId = _clientId1,
                WorkspaceId = _workspaceId1,
                TenantId = _tenantId,
                ClientSecret = "encrypted-secret-1"
            };

            var connection2 = new Entities.Connection
            {
                Id = _connectionId2,
                Name = "Staging Analytics",
                CreatedAt = _createdAt,
                ClientId = _clientId2,
                WorkspaceId = _workspaceId2,
                TenantId = _tenantId,
                ClientSecret = "encrypted-secret-2"
            };

            var connectionList = new List<Entities.Connection>
            {
                connection1,
                connection2
            };

            var user = new Entities.User
            {
                Id = _userId,
                UserName = "John Doe",
                Email = "john.doe@company.com",
                CreatedAt = _createdAt,
                ActiveConnectionId = _connectionId1
            };

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetAllAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((
                    connectionList,
                    connectionList.Count));

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    _connectionId1,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(connection1);

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    _connectionId2,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(connection2);

            _unitOfWork
                .Setup(x => x.ConnectionRepository.CheckDuplicateWorkspaceIdAsync(
                    _workspaceId1,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(connection1);

            _unitOfWork
                .Setup(x => x.ConnectionRepository.CheckDuplicateWorkspaceIdAsync(
                    _workspaceId2,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(connection2);

            _unitOfWork
                .Setup(x => x.ConnectionRepository.CheckDuplicateWorkspaceIdAsync(
                    It.Is<Guid>(id =>
                        id != _workspaceId1 &&
                        id != _workspaceId2),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Entities.Connection?)null);

            _unitOfWork
                .Setup(x => x.ConnectionRepository.AddAsync(
                    It.IsAny<Entities.Connection>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _unitOfWork
                .Setup(x => x.ConnectionRepository.Update(
                    It.IsAny<Entities.Connection>()));

            _unitOfWork
                .Setup(x => x.ConnectionRepository.Delete(
                    It.IsAny<Entities.Connection>()));

            _unitOfWork
                .Setup(x => x.UserRepository.GetByIdAsync(
                    _userId,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _unitOfWork
                .Setup(x => x.UserRepository.GetByIdWithConnectionAsync(
                    _userId,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _secretProtector
                .Setup(x => x.Protect(It.IsAny<string>()))
                .Returns((string secret) => $"protected-{secret}");

            _cacheService
                .Setup(x => x.CreateCacheKey(It.IsAny<string[]>()))
                .Returns((string[] keys) => $"connection:{keys[0]}");

            _cacheService
                .Setup(x => x.SetValueAsync(
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    It.IsAny<TimeSpan?>()))
                .Returns(Task.CompletedTask);

            _unitOfWork
                .Setup(x => x.SaveChangesAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
        }

        protected void SetUpHttpContext()
        {
            var claims = new List<Claim>
            {
                new("oid", _userId.ToString())
            };

            var identity = new ClaimsIdentity(
                claims,
                authenticationType: "Test");

            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            _httpContext
                .Setup(x => x.HttpContext)
                .Returns(httpContext);
        }
    }
}
