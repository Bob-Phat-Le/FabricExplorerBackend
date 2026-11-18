using FabricExplorerBackend.Domain.Entities.Abstractions;

namespace FabricExplorerBackend.Domain.Entities
{
    /// <summary>One entry of the "Recent Operations" history of a Landing Zone.</summary>
    public class LandingZoneOperation : BaseEntity
    {
        public const int OperationMaxLength = 64;
        public const int ItemMaxLength = 512;
        public const int PathMaxLength = 1024;
        public const int StatusMaxLength = 32;
        public const int DetailsMaxLength = 2000;

        public Guid ConnectionId { get; set; }

        public string Operation { get; set; } = null!;
        public string Item { get; set; } = null!;
        public string Path { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? Details { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public static LandingZoneOperation Create(
            Guid connectionId, string operation, string item, string path, bool success, string? details, DateTimeOffset createdAt)
            => new()
            {
                Id = Guid.NewGuid(),
                ConnectionId = connectionId,
                Operation = Truncate(operation, OperationMaxLength)!,
                Item = Truncate(item, ItemMaxLength)!,
                Path = Truncate(path, PathMaxLength)!,
                Status = success ? OperationStatuses.Completed : OperationStatuses.Failed,
                Details = Truncate(details, DetailsMaxLength),
                CreatedAt = createdAt
            };

        private static string? Truncate(string? value, int max)
            => value is { Length: > 0 } && value.Length > max ? value[..max] : value;
    }

    public static class OperationStatuses
    {
        public const string Completed = "Completed";
        public const string Failed = "Failed";
    }
}
