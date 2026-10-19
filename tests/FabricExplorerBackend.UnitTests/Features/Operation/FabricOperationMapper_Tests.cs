using FabricExplorerBackend.Application.Operation;
using FabricExplorerBackend.Application.Operation.Contracts;
using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.UnitTests.Common;

namespace FabricExplorerBackend.UnitTests.Features.Operation
{
    public class FabricOperationMapper_Tests
    {
        private readonly FabricOperationMapper _mapper = new();

        [Fact]
        public void Map_FromResponse_WithRetryAfterHeader_MapsCorrectly()
        {
            // Arrange
            var operationId = Guid.NewGuid();
            var locationUrl = "https://api.fabric.microsoft.com/v1/operations/" + operationId;
            var headers = new Dictionary<string, string>
            {
                { "x-ms-operation-id", operationId.ToString() },
                { "Location", locationUrl },
                { "Retry-After", "12" }
            };

            var response = new TestResponse(202, headers);

            // Act
            var result = _mapper.Map(response);

            // Assert
            Assert.Equal(operationId, result.OperationId);
            Assert.Equal(new Uri(locationUrl), result.OperationUrl);
            Assert.Equal(TimeSpan.FromSeconds(12), result.RetryAfter);
        }

        [Fact]
        public void Map_FromResponse_WithoutRetryAfterHeader_UsesDefault5Seconds()
        {
            // Arrange
            var operationId = Guid.NewGuid();
            var locationUrl = "https://api.fabric.microsoft.com/v1/operations/" + operationId;
            var headers = new Dictionary<string, string>
            {
                { "x-ms-operation-id", operationId.ToString() },
                { "Location", locationUrl }
            };

            var response = new TestResponse(202, headers);

            // Act
            var result = _mapper.Map(response);

            // Assert
            Assert.Equal(operationId, result.OperationId);
            Assert.Equal(new Uri(locationUrl), result.OperationUrl);
            Assert.Equal(TimeSpan.FromSeconds(5), result.RetryAfter);
        }

        [Fact]
        public void Map_FromCreateRequest_InitializesEntityFieldsCorrectly()
        {
            // Arrange
            var before = DateTimeOffset.UtcNow;
            var request = new CreateFabricOperationRequest
            {
                OperationId = Guid.NewGuid(),
                OperationUrl = new Uri("https://api.fabric.microsoft.com/v1/operations/test"),
                OperationType = "StartMirroring",
                ConnectionId = Guid.NewGuid(),
                WorkspaceId = Guid.NewGuid(),
                ResourceId = Guid.NewGuid(),
                RetryAfter = TimeSpan.FromSeconds(10)
            };

            // Act
            var entity = _mapper.Map(request);
            var after = DateTimeOffset.UtcNow;

            // Assert
            Assert.NotEqual(Guid.Empty, entity.Id);
            Assert.Equal(request.OperationId, entity.FabricOperationId);
            Assert.Equal(request.OperationUrl.ToString(), entity.FabricOperationUrl);
            Assert.Equal(request.OperationType, entity.OperationType);
            Assert.Equal(request.ConnectionId, entity.ConnectionId);
            Assert.Equal(request.WorkspaceId, entity.WorkspaceId);
            Assert.Equal(request.ResourceId, entity.ResourceId);
            Assert.Equal(FabricOperationStatus.Pending, entity.Status);
            Assert.InRange(entity.CreatedAt, before, after);
            Assert.True(entity.NextPollAt >= before.Add(request.RetryAfter));
            Assert.True(entity.NextPollAt <= after.Add(request.RetryAfter));
        }

        [Fact]
        public void Map_FromEntity_MapsToFabricOperationResponse()
        {
            // Arrange
            var entity = new FabricOperation
            {
                Id = Guid.NewGuid(),
                OperationType = "StopMirroring",
                ResourceId = Guid.NewGuid(),
                Status = FabricOperationStatus.Failed,
                PercentComplete = 50,
                ErrorCode = "MirroringFailed",
                ErrorMessage = "Connection reset by peer",
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                UpdatedAt = DateTimeOffset.UtcNow
            };

            // Act
            var result = _mapper.Map(entity);

            // Assert
            Assert.Equal(entity.Id, result.Id);
            Assert.Equal(entity.OperationType, result.OperationType);
            Assert.Equal(entity.ResourceId, result.ResourceId);
            Assert.Equal(entity.Status, result.Status);
            Assert.Equal(entity.PercentComplete, result.PercentComplete);
            Assert.Equal(entity.ErrorCode, result.ErrorCode);
            Assert.Equal(entity.ErrorMessage, result.ErrorMessage);
            Assert.Equal(entity.CreatedAt, result.CreatedAt);
            Assert.Equal(entity.UpdatedAt, result.UpdatedAt);
        }
    }
}
