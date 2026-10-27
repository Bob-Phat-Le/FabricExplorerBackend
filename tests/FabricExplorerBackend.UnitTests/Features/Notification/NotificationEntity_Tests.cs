using FabricExplorerBackend.Application.Notification.Contracts;
using FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.UnitTests.Features.Notification
{
    public class NotificationEntity_Tests
    {
        [Fact]
        public void Notification_DefaultValues_AreCorrect()
        {
            var notification = new Domain.Entities.Notification();

            Assert.Equal(Guid.Empty, notification.Id);
            Assert.Equal(Guid.Empty, notification.UserId);
            Assert.False(notification.IsRead);
            Assert.Null(notification.ReadAt);
            Assert.Null(notification.OperationId);
            Assert.Null(notification.Message);
            Assert.Null(notification.Operation);
        }

        [Fact]
        public void Notification_PropertiesSet_ArePreserved()
        {
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var operationId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;

            var notification = new Domain.Entities.Notification
            {
                Id = id,
                UserId = userId,
                Type = NotificationType.Connection.CreateConnectionSucceeded,
                Title = "Connection created",
                Message = "Customer DB connection was created successfully.",
                OperationId = operationId,
                IsRead = false,
                CreatedAt = now,
                ReadAt = null
            };

            Assert.Equal(id, notification.Id);
            Assert.Equal(userId, notification.UserId);
            Assert.Equal(NotificationType.Connection.CreateConnectionSucceeded, notification.Type);
            Assert.Equal("Connection created", notification.Title);
            Assert.Equal("Customer DB connection was created successfully.", notification.Message);
            Assert.Equal(operationId, notification.OperationId);
            Assert.False(notification.IsRead);
            Assert.Equal(now, notification.CreatedAt);
            Assert.Null(notification.ReadAt);
        }

        [Fact]
        public void Notification_MarkAsRead_SetsIsReadAndReadAt()
        {
            var notification = new Domain.Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Type = NotificationType.Connection.CreateConnectionSucceeded,
                Title = "Connection created",
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                IsRead = false,
                ReadAt = null
            };

            var before = DateTimeOffset.UtcNow;
            notification.MarkAsRead();
            var after = DateTimeOffset.UtcNow;

            Assert.True(notification.IsRead);
            Assert.NotNull(notification.ReadAt);
            Assert.True(notification.ReadAt >= before && notification.ReadAt <= after);
        }

        [Fact]
        public void Notification_MarkAsRead_WithCustomTimestamp_SetsSpecifiedReadAt()
        {
            var notification = new Domain.Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Type = NotificationType.Operation.OperationSucceeded,
                Title = "Load table completed",
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
                IsRead = false,
                ReadAt = null
            };

            var customReadAt = DateTimeOffset.UtcNow.AddMinutes(-10);
            notification.MarkAsRead(customReadAt);

            Assert.True(notification.IsRead);
            Assert.Equal(customReadAt, notification.ReadAt);
        }

        [Fact]
        public void NotificationType_ConnectionConstants_HaveExpectedValues()
        {
            Assert.Equal("connection.created.succeeded", NotificationType.Connection.CreateConnectionSucceeded);
            Assert.Equal("connection.created.failed", NotificationType.Connection.CreateConnectionFailed);
            Assert.Equal("connection.deleted.succeeded", NotificationType.Connection.DeleteConnectionSucceeded);
            Assert.Equal("connection.deleted.failed", NotificationType.Connection.DeleteConnectionFailed);
            Assert.Equal("connection.updated.succeeded", NotificationType.Connection.UpdateConnectionSucceeded);
            Assert.Equal("connection.updated.failed", NotificationType.Connection.UpdateConnectionFailed);
            Assert.Equal("connection.tested.succeeded", NotificationType.Connection.TestConnectionSucceeded);
            Assert.Equal("connection.tested.failed", NotificationType.Connection.TestConnectionFailed);
        }

        [Fact]
        public void NotificationType_OperationConstants_HaveExpectedValues()
        {
            Assert.Equal("operation.succeeded", NotificationType.Operation.OperationSucceeded);
            Assert.Equal("operation.failed", NotificationType.Operation.OperationFailed);
        }
    }
}
