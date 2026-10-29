using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Features.Notification;
using FabricExplorerBackend.Features.Notification.Contracts;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace FabricExplorerBackend.UnitTests.Features.Notification
{
    public class NotificationService_Tests
    {
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<INotificationMapper> _mapper = new();
        private readonly Mock<ILogger<NotificationService>> _logger = new();
        private readonly INotificationService _service;

        public NotificationService_Tests()
        {
            _service = new NotificationService(_unitOfWork.Object, _mapper.Object, _logger.Object);
        }

        [Fact]
        public async Task GetNotificationByIdAsync_WhenExists_ReturnsSuccess()
        {
            var id = Guid.NewGuid();
            var notification = new Domain.Entities.Notification
            {
                Id = id,
                UserId = Guid.NewGuid(),
                Type = NotificationType.Connection.CreateConnectionSucceeded,
                Title = "Connection created"
            };

            var expectedResponse = new NotificationResponse
            {
                Id = id,
                UserId = notification.UserId,
                Type = notification.Type,
                Title = notification.Title
            };

            _unitOfWork
                .Setup(u => u.NotificationRepository.GetByIdAsync(id, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(notification);

            _mapper
                .Setup(m => m.Map(notification))
                .Returns(expectedResponse);

            var result = await _service.GetNotificationByIdAsync(id);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(id, result.Data.Id);
            Assert.Equal(notification.Title, result.Data.Title);
        }

        [Fact]
        public async Task GetNotificationByIdAsync_WhenNotFound_ReturnsNotFoundError()
        {
            var id = Guid.NewGuid();

            _unitOfWork
                .Setup(u => u.NotificationRepository.GetByIdAsync(id, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Notification?)null);

            var result = await _service.GetNotificationByIdAsync(id);

            Assert.False(result.IsSuccess);
            Assert.NotEmpty(result.Errors);
            Assert.Equal(ErrorCodes.Notification.NotFound, result.Errors[0].Code);
        }

        [Fact]
        public async Task ListNotificationAsync_ReturnsMappedNotifications()
        {
            var notifications = new List<Domain.Entities.Notification>
            {
                new() { Id = Guid.NewGuid(), Title = "Notification 1", Type = NotificationType.Connection.CreateConnectionSucceeded },
                new() { Id = Guid.NewGuid(), Title = "Notification 2", Type = NotificationType.Connection.DeleteConnectionSucceeded }
            };

            _unitOfWork
                .Setup(u => u.NotificationRepository.GetAllAsync(0, 50, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync((notifications, notifications.Count));

            _mapper
                .Setup(m => m.Map(It.IsAny<Domain.Entities.Notification>()))
                .Returns((Domain.Entities.Notification n) => new NotificationResponse { Id = n.Id, Title = n.Title, Type = n.Type });

            var result = await _service.ListNotificationAsync();

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.Count);
        }

        [Fact]
        public async Task GetNotificationsByUserIdAsync_ReturnsUserNotifications()
        {
            var userId = Guid.NewGuid();
            var notifications = new List<Domain.Entities.Notification>
            {
                new() { Id = Guid.NewGuid(), UserId = userId, Title = "User Notification", Type = NotificationType.Connection.CreateConnectionSucceeded }
            };

            _unitOfWork
                .Setup(u => u.NotificationRepository.GetByUserIdAsync(userId, 0, 50, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync((notifications, notifications.Count));

            _mapper
                .Setup(m => m.Map(It.IsAny<Domain.Entities.Notification>()))
                .Returns((Domain.Entities.Notification n) => new NotificationResponse { Id = n.Id, UserId = n.UserId, Title = n.Title, Type = n.Type });

            var result = await _service.GetNotificationsByUserIdAsync(userId);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Single(result.Data);
            Assert.Equal(userId, result.Data[0].UserId);
        }
    }
}
