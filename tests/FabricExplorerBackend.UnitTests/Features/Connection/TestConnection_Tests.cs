using Azure;
using FabricExplorerBackend.Infrastructures.Fabric.Context;
using FabricExplorerBackend.UnitTests.Common;
using FabricExplorerBackend.UnitTests.Features.Connection.Abstraction;
using Moq;
using Entities = FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.UnitTests.Features.Connection
{
    public class TestConnection_Tests : ConnectionServiceTestBase
    {
        [Fact]
        public async Task TestConnection_WhenConnectionDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    id,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Entities.Connection?)null);

            // Act
            var result = await _service.TestConnection(id);

            // Assert
            Assert.False(result.IsSuccess);
            _tokenService.Verify(x =>
                x.CreateTokenAsync(It.IsAny<Entities.Connection>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task TestConnection_WhenWorkspaceIdMismatches_ReturnsNotFound()
        {
            // Arrange
            var connection = new Entities.Connection
            {
                Id = _connectionId1,
                WorkspaceId = _workspaceId1,
                TenantId = _tenantId,
                ClientId = _clientId1,
                ClientSecret = "secret"
            };

            var (fabricClient, mockWorkspaces, _, _, _, _, _) = FabricTestHelper.CreateMockFabricClientHierarchy();
            var differentWorkspaceId = Guid.NewGuid();
            var differentWorkspace = FabricTestHelper.CreateWorkspaceInfo(differentWorkspaceId, "Different Workspace");

            mockWorkspaces
                .Setup(x => x.GetWorkspaceAsync(connection.WorkspaceId, It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(differentWorkspace, Mock.Of<Response>()));

            var context = new FabricContext
            {
                ConnectionId = connection.Id,
                WorkspaceId = connection.WorkspaceId,
                TenantId = connection.TenantId,
                Client = fabricClient
            };

            _tokenService
                .Setup(x => x.CreateTokenAsync(connection, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("valid-token");

            _contextFactory
                .Setup(x => x.CreateFabricContextWithTokenAsync(It.IsAny<Entities.Connection>(), It.IsAny<string>()))
                .ReturnsAsync(context);

            // Act
            var result = await _service.TestConnection(_connectionId1);

            // Assert
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task TestConnection_WhenWorkspaceMatches_ReturnsSuccess()
        {
            // Arrange
            var connection = new Entities.Connection
            {
                Id = _connectionId1,
                WorkspaceId = _workspaceId1,
                TenantId = _tenantId,
                ClientId = _clientId1,
                ClientSecret = "secret"
            };

            var (fabricClient, mockWorkspaces, _, _, _, _, _) = FabricTestHelper.CreateMockFabricClientHierarchy();
            var workspace = FabricTestHelper.CreateWorkspaceInfo(_workspaceId1, "Production Workspace");

            mockWorkspaces
                .Setup(x => x.GetWorkspaceAsync(connection.WorkspaceId, It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(workspace, Mock.Of<Response>()));

            var context = new FabricContext
            {
                ConnectionId = connection.Id,
                WorkspaceId = connection.WorkspaceId,
                TenantId = connection.TenantId,
                Client = fabricClient
            };

            _tokenService
                .Setup(x => x.CreateTokenAsync(connection, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("valid-token");

            _contextFactory
                .Setup(x => x.CreateFabricContextWithTokenAsync(It.IsAny<Entities.Connection>(), It.IsAny<string>()))
                .ReturnsAsync(context);

            // Act
            var result = await _service.TestConnection(_connectionId1);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("Connection check succeeded", result.Data!.Message);
            Assert.Contains("Production Workspace", result.Data.Message);
        }
    }
}
