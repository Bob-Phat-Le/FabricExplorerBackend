using FabricExplorerBackend.Domain.Entities.Abstractions;
using FabricExplorerBackend.Domain.Enums;

namespace FabricExplorerBackend.Domain.Entities
{
    /// <summary>
    /// Logical query + its lifecycle. The result itself is NOT stored here (it lives in the result store);
    /// the physical location is always derived from <see cref="BaseEntity.Id"/>.
    /// </summary>
    public class QueryMetadata : BaseEntity, IAuditable
    {
        public const int ErrorMaxLength = 2000;

        public string Sql { get; set; } = null!;
        public QueryStatus Status { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public long RowCount { get; set; }
        public string? Error { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid ConnectionId { get; set; }
        public Connection Connection { get; set; } = null!;

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public Guid? UpdatedBy { get; set; }

        public bool IsTerminal => Status is QueryStatus.Completed or QueryStatus.Failed or QueryStatus.Cancelled;

        public static QueryMetadata Create(Guid userId, Guid connectionId, string sql, DateTimeOffset now, TimeSpan retention)
            => new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ConnectionId = connectionId,
                Sql = sql,
                Status = QueryStatus.Pending,
                CreatedAt = now,
                CreatedBy = userId,
                ExpiresAt = now + retention
            };

        public void MarkCompleted(long rowCount, DateTimeOffset now, TimeSpan retention)
        {
            Status = QueryStatus.Completed;
            RowCount = rowCount;
            Error = null;
            CompletedAt = now;
            UpdatedAt = now;
            // The retention window of the result starts when the result becomes available.
            ExpiresAt = now + retention;
        }

        public void MarkFailed(string error, DateTimeOffset now)
        {
            Status = QueryStatus.Failed;
            Error = error.Length > ErrorMaxLength ? error[..ErrorMaxLength] : error;
            CompletedAt = now;
            UpdatedAt = now;
        }

        public void MarkCancelled(DateTimeOffset now)
        {
            Status = QueryStatus.Cancelled;
            CompletedAt = now;
            UpdatedAt = now;
        }
    }
}
