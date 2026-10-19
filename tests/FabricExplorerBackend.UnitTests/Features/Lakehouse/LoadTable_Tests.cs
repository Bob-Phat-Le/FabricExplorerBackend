using FabricExplorerBackend.Application.Lakehouse.Contracts;
using FabricExplorerBackend.Application.Operation.Contracts;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Infrastructures.Fabric.Context;
using FabricExplorerBackend.UnitTests.Common;
using FabricExplorerBackend.UnitTests.Features.Lakehouse.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.Fabric.Api.Lakehouse.Models;
using Moq;
using Entities = FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.UnitTests.Features.Lakehouse
{
    public class LoadTable_Tests : LakehouseServiceTestBase
    {
        [Fact]
        public async Task LoadTableAsync_WhenConnectionNotFound_ReturnsNotFound()
        {
            // Arrange
            var connectionId = Guid.NewGuid();
            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    connectionId,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Entities.Connection?)null);

            var request = new ApplicationLoadTableRequest
            {
                RelativePath = "data/file.csv",
                PathType = "File",
                Mode = "Append",
                FileExtension = "csv",
                FormatOptions = System.Text.Json.JsonDocument.Parse("{\"format\":\"csv\"}").RootElement
            };

            // Act
            var result = await _service.LoadTableAsync(connectionId, _workspaceId, _lakehouseId, "MyTable", request);

            // Assert
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task LoadTableAsync_WhenResponseIs200OK_ReturnsCompletedWithNullOperationId()
        {
            // Arrange
            var connection = new Entities.Connection { Id = _connectionId };

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    _connectionId,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(connection);

            var (fabricClient, _, _, _, mockTables, _, _) = FabricTestHelper.CreateMockFabricClientHierarchy();

            var response200 = new TestResponse(StatusCodes.Status200OK);
            mockTables
                .Setup(x => x.LoadTableAsync(
                    _workspaceId,
                    _lakehouseId,
                    "MyTable",
                    It.IsAny<LoadTableRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response200);

            var context = new FabricContext
            {
                ConnectionId = _connectionId,
                Client = fabricClient
            };

            _fabricContextFactory
                .Setup(x => x.CreateFabricContextWithoutTokenAsync(connection, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context);

            var request = new ApplicationLoadTableRequest
            {
                RelativePath = "data/file.csv",
                PathType = "File",
                Mode = "Append",
                FileExtension = "csv",
                FormatOptions = System.Text.Json.JsonDocument.Parse("{\"format\":\"csv\"}").RootElement
            };

            // Act
            var result = await _service.LoadTableAsync(_connectionId, _workspaceId, _lakehouseId, "MyTable", request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Null(result.Data.OperationId);
            Assert.Equal("Table load completed.", result.Data.Message);

            _fabricOperationService.Verify(x =>
                x.CreateFabricOperationAsync(It.IsAny<CreateFabricOperationRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task LoadTableAsync_WhenResponseIs202Accepted_TracksOperationAndReturnsOperationId()
        {
            // Arrange
            var connection = new Entities.Connection { Id = _connectionId };

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    _connectionId,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(connection);

            var (fabricClient, _, _, _, mockTables, _, _) = FabricTestHelper.CreateMockFabricClientHierarchy();

            var headers = new Dictionary<string, string>
            {
                ["x-ms-operation-id"] = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                ["Location"] = "https://api.fabric.microsoft.com/v1/operations/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
            };
            var response202 = new TestResponse(StatusCodes.Status202Accepted, headers);

            mockTables
                .Setup(x => x.LoadTableAsync(
                    _workspaceId,
                    _lakehouseId,
                    "MyTable",
                    It.IsAny<LoadTableRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response202);

            var context = new FabricContext
            {
                ConnectionId = _connectionId,
                Client = fabricClient
            };

            _fabricContextFactory
                .Setup(x => x.CreateFabricContextWithoutTokenAsync(connection, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context);

            var opReq = new CreateFabricOperationRequest();
            _fabricOperationMapper
                .Setup(x => x.Map(response202))
                .Returns(opReq);

            var createdOpId = Guid.NewGuid();
            var opResponse = new FabricOperationResponse { Id = createdOpId };
            _fabricOperationService
                .Setup(x => x.CreateFabricOperationAsync(opReq, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<FabricOperationResponse>.Success(opResponse));

            var request = new ApplicationLoadTableRequest
            {
                RelativePath = "data/file.csv",
                PathType = "File",
                Mode = "Append",
                FileExtension = "csv",
                FormatOptions = System.Text.Json.JsonDocument.Parse("{\"format\":\"csv\"}").RootElement
            };

            // Act
            var result = await _service.LoadTableAsync(_connectionId, _workspaceId, _lakehouseId, "MyTable", request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(createdOpId, result.Data.OperationId);
            Assert.Contains("Load table accepted", result.Data.Message);
        }

        [Fact]
        public async Task LoadTableAsync_WhenOperationTrackingThrows_ReturnsSuccessWithNullOperationId()
        {
            // Arrange
            var connection = new Entities.Connection { Id = _connectionId };

            _unitOfWork
                .Setup(x => x.ConnectionRepository.GetByIdAsync(
                    _connectionId,
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(connection);

            var (fabricClient, _, _, _, mockTables, _, _) = FabricTestHelper.CreateMockFabricClientHierarchy();

            var response202 = new TestResponse(StatusCodes.Status202Accepted);

            mockTables
                .Setup(x => x.LoadTableAsync(
                    _workspaceId,
                    _lakehouseId,
                    "MyTable",
                    It.IsAny<LoadTableRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response202);

            var context = new FabricContext
            {
                ConnectionId = _connectionId,
                Client = fabricClient
            };

            _fabricContextFactory
                .Setup(x => x.CreateFabricContextWithoutTokenAsync(connection, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context);

            _fabricOperationMapper
                .Setup(x => x.Map(response202))
                .Throws(new InvalidOperationException("Tracking failed"));

            var request = new ApplicationLoadTableRequest
            {
                RelativePath = "data/file.csv",
                PathType = "File",
                Mode = "Append",
                FileExtension = "csv",
                FormatOptions = System.Text.Json.JsonDocument.Parse("{\"format\":\"csv\"}").RootElement
            };

            // Act
            var result = await _service.LoadTableAsync(_connectionId, _workspaceId, _lakehouseId, "MyTable", request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Null(result.Data.OperationId);
            Assert.Contains("progress cannot be tracked", result.Data.Message);
        }
    }
}
