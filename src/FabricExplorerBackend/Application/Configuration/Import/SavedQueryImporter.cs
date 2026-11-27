using FabricExplorerBackend.Application.Configuration.Contracts;
using FabricExplorerBackend.Application.Configuration.Documents;
using FabricExplorerBackend.Application.SavedQueries;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;

namespace FabricExplorerBackend.Application.Configuration.Import
{
    /// <summary>Writes one validated saved query entry for the importing user.</summary>
    public interface ISavedQueryImporter
    {
        Task<ImportResultItem> ImportAsync(SavedQueryEntry entry, ImportSession session, CancellationToken cancellationToken);
    }

    public sealed class SavedQueryImporter(
        IUnitOfWork unitOfWork,
        TimeProvider clock,
        ISavedQueryMapper mapper) : ISavedQueryImporter
    {
        public async Task<ImportResultItem> ImportAsync(SavedQueryEntry entry, ImportSession session, CancellationToken cancellationToken)
        {
            var type = ConfigurationEntryTypes.SavedQuery;
            var key = Guid.Parse(entry.Key!);
            var normalizedKey = key.ToString("D");
            var name = entry.Name!.Trim();

            if (session.References.Resolve(entry.ConnectionKey, out var connectionId) != ReferenceState.Existing)
                return ImportResultItem.Unresolved(type, normalizedKey, name, "Connection is not in the file or in the system, or was not imported.");

            var repository = unitOfWork.SavedQueryRepository;
            var existing = await repository.FindByKeyAsync(session.UserId, key, true, cancellationToken)
                ?? await repository.FindByNameAsync(session.UserId, connectionId, name, true, cancellationToken);

            var action = session.Mode.Decide(existing is not null);

            if (action == ImportAction.Skip)
                return ImportResultItem.Succeeded(type, normalizedKey, name, ImportResultItem.Skipped, existing!.Id, []);

            var now = clock.GetUtcNow();
            SavedQuery query;

            if (action == ImportAction.Create)
            {
                query = new SavedQuery
                {
                    Id = Guid.NewGuid(),
                    ExternalKey = key,
                    OwnerId = session.UserId,
                    ConnectionId = connectionId,
                    CreatedAt = now
                };
                mapper.Apply(entry, query);
                await repository.AddAsync(query, cancellationToken);
            }
            else
            {
                query = existing!;

                var clash = await repository.FindByNameAsync(session.UserId, connectionId, name, false, cancellationToken);
                if (clash is not null && clash.Id != query.Id)
                    return ImportResultItem.Failure(type, normalizedKey, name, ErrorCodes.SavedQuery.Duplicated, "Another saved query of this connection has the same name.");

                query.ExternalKey = key;
                query.ConnectionId = connectionId;
                query.UpdatedAt = now;
                mapper.Apply(entry, query);
                repository.Update(query);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ImportResultItem.Succeeded(
                type, normalizedKey, query.Name,
                action == ImportAction.Create ? ImportResultItem.Created : ImportResultItem.Updated,
                query.Id, []);
        }
    }
}
