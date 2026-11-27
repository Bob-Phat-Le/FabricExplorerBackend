using FabricExplorerBackend.Application.Configuration.Documents;
using FabricExplorerBackend.Application.Configuration.Validation;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;

namespace FabricExplorerBackend.Application.Configuration.Import
{
    public sealed class ConnectionReferenceBuilder(IUnitOfWork unitOfWork) : IConnectionReferenceBuilder
    {
        public async Task<Result<ConnectionReferences>> BuildAsync(
            ConfigurationDocument document, IReadOnlyDictionary<string, Guid>? mappings, CancellationToken cancellationToken)
        {
            var references = new ConnectionReferences();
            var (connections, _) = await unitOfWork.ConnectionRepository.GetAllAsync(0, 0, cancellationToken: cancellationToken);

            // Put every existing connection into the Known dictionary, keyed by its normalized external key.
            foreach (var connection in connections)
                references.Known[connection.ExternalKey.ToString("D")] = connection.Id;

            // This is just in case with the same workspaceId, but 2 environments store with different external keys
            var byWorkspace = connections.ToDictionary(c => c.WorkspaceId, c => c.Id);

            // A connection of the file that matches an existing one by workspace stands for that one.
            foreach (var entry in document.Connections ?? [])
            {
                // This just parse key into guid
                if (!ConfigurationGuid.TryParse(entry.Key, out var key))
                    continue;

                var normalized = key.ToString("D");
                // if Known contains key then skip it (this means that key is already known in database)
                if (references.Known.ContainsKey(normalized))
                    continue;

                // This is in case the entry.Key is different from the external key in database, we will map this key to the already existed id via workspaceId
                if (ConfigurationGuid.TryParse(entry.WorkspaceId, out var workspaceId) && byWorkspace.TryGetValue(workspaceId, out var existingId))
                    references.Known[normalized] = existingId;
                else
                    references.Pending.Add(normalized);
            }

            foreach (var (fileKey, connectionId) in mappings ?? new Dictionary<string, Guid>())
            {
                if (!ConfigurationGuid.TryParse(fileKey, out var parsedKey))
                    return Error.BadRequest(ErrorCodes.Configuration.InvalidMapping, $"'{fileKey}' is not a valid connection key.");

                if (!connections.Any(c => c.Id == connectionId))
                    return Error.BadRequest(ErrorCodes.Configuration.InvalidMapping, $"Connection '{connectionId}' does not exist.");

                var normalized = parsedKey.ToString("D");
                references.Known[normalized] = connectionId;
                references.Pending.Remove(normalized);
            }

            return Result<ConnectionReferences>.Success(references);
        }
    }
}
