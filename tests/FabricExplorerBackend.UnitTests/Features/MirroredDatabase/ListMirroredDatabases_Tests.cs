using Azure;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.UnitTests.Common;
using FabricExplorerBackend.UnitTests.Features.MirroredDatabase.Abstraction;
using Moq;

namespace FabricExplorerBackend.UnitTests.Features.MirroredDatabase
{
    public class ListMirroredDatabases_Tests : MirroredDatabaseServiceTestBase
    {
        [Fact]
        public async Task ListMirroredDatabasesAsync_WhenConnectionNotFound_ReturnsNotFoundError()
        {
            // Arrange
            _connectionRepository
                .Setup(r => r.GetByIdAsync(_connectionId, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Connection?)null);

            // Act
            var result = await _service.ListMirroredDatabasesAsync(_connectionId, _workspaceId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains(result.Errors, e => e.Code == ErrorCodes.Connection.NotFound);
        }

        [Fact]
        public async Task ListMirroredDatabasesAsync_WhenValidRequest_ReturnsAlphabeticallySortedList()
        {
            // Arrange
            var connection = SetupConnection();

            var workspaceInfo = FabricTestHelper.CreateWorkspaceInfo(_workspaceId, "Analytics Workspace");
            _mockWorkspaces
                .Setup(w => w.GetWorkspaceAsync(_workspaceId, It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(workspaceInfo, Mock.Of<Response>()));

            var dbBetaId = Guid.NewGuid();
            var dbAlphaId = Guid.NewGuid();
            var dbBeta = FabricTestHelper.CreateMirroredDatabase(dbBetaId, "BetaDatabase");
            var dbAlpha = FabricTestHelper.CreateMirroredDatabase(dbAlphaId, "AlphaDatabase");

            var pageable = FabricTestHelper.CreateAsyncPageable(new[] { dbBeta, dbAlpha });

            _mockMirroredDbItems
                .Setup(i => i.ListMirroredDatabasesAsync(_workspaceId, It.IsAny<bool?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .Returns(pageable);

            // Act
            var result = await _service.ListMirroredDatabasesAsync(_connectionId, _workspaceId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);

            var list = result.Data.ToList();
            Assert.Equal(2, list.Count);
            Assert.Equal("AlphaDatabase", list[0].MirroredDatabaseName);
            Assert.Equal(dbAlphaId, list[0].MirroredDatabaseId);
            Assert.Equal(_workspaceId, list[0].WorkspaceId);
            Assert.Equal("Analytics Workspace", list[0].WorkspaceName);
            Assert.Equal(_connectionId, list[0].ConnectionId);

            Assert.Equal("BetaDatabase", list[1].MirroredDatabaseName);
            Assert.Equal(dbBetaId, list[1].MirroredDatabaseId);
        }

        [Fact]
        public async Task ListMirroredDatabasesAsync_WhenBypassCacheIsTrue_PassesFlagToCachedFetcher()
        {
            // Arrange
            var connection = SetupConnection();

            var workspaceInfo = FabricTestHelper.CreateWorkspaceInfo(_workspaceId, "Analytics Workspace");
            _mockWorkspaces
                .Setup(w => w.GetWorkspaceAsync(_workspaceId, It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(workspaceInfo, Mock.Of<Response>()));

            var pageable = FabricTestHelper.CreateAsyncPageable(Array.Empty<Microsoft.Fabric.Api.MirroredDatabase.Models.MirroredDatabase>());

            _mockMirroredDbItems
                .Setup(i => i.ListMirroredDatabasesAsync(_workspaceId, It.IsAny<bool?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .Returns(pageable);

            // Act
            var result = await _service.ListMirroredDatabasesAsync(_connectionId, _workspaceId, bypassCache: true);

            // Assert
            Assert.True(result.IsSuccess);
            _cachedFetcher.Verify(x => x.GetOrFetchAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<Func<CancellationToken, Task<IReadOnlyList<FabricExplorerBackend.Application.MirroredDatabase.Contracts.MirroredDatabaseListItemResponse>>>>(),
                true,
                It.IsAny<Func<IReadOnlyList<FabricExplorerBackend.Application.MirroredDatabase.Contracts.MirroredDatabaseListItemResponse>, bool>?>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
