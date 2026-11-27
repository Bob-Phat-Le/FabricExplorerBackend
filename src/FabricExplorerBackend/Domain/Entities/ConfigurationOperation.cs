using FabricExplorerBackend.Domain.Entities.Abstractions;

namespace FabricExplorerBackend.Domain.Entities
{
    /// <summary>
    /// One import or export of configuration. Doubles as the idempotency record of an import:
    /// the response of an import sent with an Idempotency-Key is kept in <see cref="ResultJson"/>.
    /// </summary>
    public class ConfigurationOperation : BaseEntity
    {
        public const int KindMaxLength = 16;
        public const int StatusMaxLength = 16;
        public const int SummaryMaxLength = 2000;
        public const int IdempotencyKeyMaxLength = 128;

        public Guid UserId { get; set; }
        public string Kind { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? Summary { get; set; }
        public int ItemCount { get; set; }
        public string? IdempotencyKey { get; set; }
        public string? ResultJson { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public static ConfigurationOperation Create(
            Guid userId, string kind, string status, string? summary, int itemCount,
            string? idempotencyKey, string? resultJson, DateTimeOffset createdAt)
            => new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Kind = kind,
                Status = status,
                Summary = summary is { Length: > SummaryMaxLength } ? summary[..SummaryMaxLength] : summary,
                ItemCount = itemCount,
                IdempotencyKey = idempotencyKey,
                ResultJson = resultJson,
                CreatedAt = createdAt
            };
    }

    public static class ConfigurationOperationKinds
    {
        public const string Import = "Import";
        public const string Export = "Export";
    }

    public static class ConfigurationOperationStatuses
    {
        public const string Completed = "Completed";
        public const string CompletedWithErrors = "CompletedWithErrors";
        public const string Failed = "Failed";
    }
}
