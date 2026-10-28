using FabricExplorerBackend.Domain.Entities.Abstractions;

namespace FabricExplorerBackend.Domain.Entities
{
    public class Notification : BaseEntity
    {
        public const int TypeMaxLength = 128;
        public const int TitleMaxLength = 256;
        public const int MessageMaxLength = 2000;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public string Type { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? Message { get; set; }

        public Guid? OperationId { get; set; }
        public FabricOperation? Operation { get; set; }

        public bool IsRead => ReadAt is not null && ReadAt.HasValue;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? ReadAt { get; set; }

        public static Notification Create(Guid userId, string type, string title, string? message, Guid? operationId, DateTimeOffset createdAt)
            => new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Title = title.Length > TitleMaxLength ? title[..TitleMaxLength] : title,
                Message = message is { Length: > MessageMaxLength } ? message[..MessageMaxLength] : message,
                OperationId = operationId,
                CreatedAt = createdAt,
                ReadAt = null
            };

        public void MarkAsRead(DateTimeOffset? readAt = null)
        {
            ReadAt = readAt ?? DateTimeOffset.UtcNow;
        }
    }
}
