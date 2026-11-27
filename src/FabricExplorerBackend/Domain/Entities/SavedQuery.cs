using FabricExplorerBackend.Domain.Entities.Abstractions;

namespace FabricExplorerBackend.Domain.Entities
{
    /// <summary>A SQL query a user saved from the Query Editor. Private to its owner.</summary>
    public class SavedQuery : BaseEntity
    {
        public const int NameMaxLength = 200;
        public const int DescriptionMaxLength = 1000;
        public const int ItemTypeMaxLength = 32;
        public const int ItemNameMaxLength = 256;
        public const int LanguageMaxLength = 16;
        public const int SqlMaxLength = 1_000_000;

        /// <summary>Stable identifier used by import/export to match entries between environments.</summary>
        public Guid ExternalKey { get; set; }
        public Guid OwnerId { get; set; }
        public Guid ConnectionId { get; set; }

        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        public string ItemType { get; set; } = null!;
        public Guid ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;

        public string Language { get; set; } = FabricQueryLanguages.TSql;
        public string Sql { get; set; } = null!;

        /// <summary>null = unlimited.</summary>
        public int? MaxRows { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    public static class FabricItemTypes
    {
        public const string Lakehouse = "Lakehouse";
        public const string Warehouse = "Warehouse";
        public const string MirroredDatabase = "MirroredDatabase";

        public static readonly IReadOnlyList<string> All = [Lakehouse, Warehouse, MirroredDatabase];
    }

    public static class FabricQueryLanguages
    {
        public const string TSql = "tsql";
    }
}
