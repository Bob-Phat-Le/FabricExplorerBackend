using FabricExplorerBackend.Application.Notification;
using FabricExplorerBackend.Features.Notification;
using FabricExplorerBackend.Features.Notification.Contracts;

namespace FabricExplorerBackend.UnitTests.Features.Notification
{
    public class NotificationMapper_Tests
    {
        private readonly INotificationMapper _mapper = new NotificationMapper();

        [Fact]
        public void Map_MapsAllPropertiesCorrectly()
        {
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var operationId = Guid.NewGuid();
            var createdAt = DateTimeOffset.UtcNow.AddMinutes(-30);
            var readAt = DateTimeOffset.UtcNow;

            var notification = new Domain.Entities.Notification
            {
                Id = id,
                UserId = userId,
                Type = NotificationType.Connection.CreateConnectionSucceeded,
                Title = "Connection created",
                Message = "Customer DB connection was created successfully.",
                OperationId = operationId,
                IsRead = true,
                CreatedAt = createdAt,
                ReadAt = readAt
            };

            var response = _mapper.Map(notification);

            Assert.NotNull(response);
            Assert.Equal(id, response.Id);
            Assert.Equal(userId, response.UserId);
            Assert.Equal(NotificationType.Connection.CreateConnectionSucceeded, response.Type);
            Assert.Equal("Connection created", response.Title);
            Assert.Equal("Customer DB connection was created successfully.", response.Message);
            Assert.Equal(operationId, response.OperationId);
            Assert.True(response.IsRead);
            Assert.Equal(createdAt, response.CreatedAt);
            Assert.Equal(readAt, response.ReadAt);
        }

        [Fact]
        public void Map_WithNullableProperties_MapsCorrectly()
        {
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var createdAt = DateTimeOffset.UtcNow;

            var notification = new Domain.Entities.Notification
            {
                Id = id,
                UserId = userId,
                Type = NotificationType.Connection.TestConnectionFailed,
                Title = "Connection test failed",
                Message = null,
                OperationId = null,
                IsRead = false,
                CreatedAt = createdAt,
                ReadAt = null
            };

            var response = _mapper.Map(notification);

            Assert.NotNull(response);
            Assert.Equal(id, response.Id);
            Assert.Equal(userId, response.UserId);
            Assert.Equal(NotificationType.Connection.TestConnectionFailed, response.Type);
            Assert.Equal("Connection test failed", response.Title);
            Assert.Null(response.Message);
            Assert.Null(response.OperationId);
            Assert.False(response.IsRead);
            Assert.Equal(createdAt, response.CreatedAt);
            Assert.Null(response.ReadAt);
        }
    }
}
