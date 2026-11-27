using FabricExplorerBackend.Application.Configuration.Documents;
using FabricExplorerBackend.Application.SavedQueries.Contracts;
using FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.Application.SavedQueries
{
    public interface ISavedQueryMapper
    {
        SavedQueryResponse ToResponse(SavedQuery query, string? connectionName);

        /// <summary>The interchange shape of a query (used for validation and export).</summary>
        SavedQueryEntry ToEntry(SavedQuery query, Guid connectionExternalKey);

        /// <summary>Copies the content of a validated entry onto the entity.</summary>
        void Apply(SavedQueryEntry entry, SavedQuery target);
    }

    public sealed class SavedQueryMapper : ISavedQueryMapper
    {
        public SavedQueryResponse ToResponse(SavedQuery query, string? connectionName)
            => new(
                query.Id, query.ExternalKey, query.Name, query.Description, query.ConnectionId, connectionName,
                new SavedQueryTargetResponse(query.ItemType, query.ItemId, query.ItemName),
                query.Language, query.Sql, query.MaxRows, query.CreatedAt, query.UpdatedAt);

        public SavedQueryEntry ToEntry(SavedQuery query, Guid connectionExternalKey)
            => new()
            {
                Key = query.ExternalKey.ToString("D"),
                Name = query.Name,
                Description = query.Description,
                ConnectionKey = connectionExternalKey.ToString("D"),
                Target = new QueryTargetEntry
                {
                    ItemType = query.ItemType,
                    ItemId = query.ItemId.ToString("D"),
                    ItemName = query.ItemName
                },
                Language = query.Language,
                Sql = query.Sql,
                MaxRows = query.MaxRows,
                CreatedAt = query.CreatedAt,
                UpdatedAt = query.UpdatedAt
            };

        public void Apply(SavedQueryEntry entry, SavedQuery target)
        {
            target.Name = entry.Name!.Trim();
            target.Description = string.IsNullOrWhiteSpace(entry.Description) ? null : entry.Description.Trim();
            target.ItemType = FabricItemTypes.All.First(t => string.Equals(t, entry.Target!.ItemType, StringComparison.OrdinalIgnoreCase));
            target.ItemId = Guid.Parse(entry.Target!.ItemId!);
            target.ItemName = entry.Target!.ItemName?.Trim() ?? string.Empty;
            target.Language = string.IsNullOrWhiteSpace(entry.Language) ? FabricQueryLanguages.TSql : entry.Language.Trim().ToLowerInvariant();
            target.Sql = entry.Sql!;
            target.MaxRows = entry.MaxRows;
        }
    }
}
