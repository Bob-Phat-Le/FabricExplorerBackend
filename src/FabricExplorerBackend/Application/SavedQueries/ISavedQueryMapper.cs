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
}
