using FabricExplorerBackend.Application.Configuration.Contracts;
using FabricExplorerBackend.Application.Configuration.Documents;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using ConnectionEntity = FabricExplorerBackend.Domain.Entities.Connection;

namespace FabricExplorerBackend.Application.Configuration.Import
{
    public sealed class ConnectionImporter(
        IUnitOfWork unitOfWork,
        TimeProvider clock,
        IEnumerable<IConnectionImportStep> steps) : IConnectionImporter
    {
        public const string NeedsCredentials = "NeedsCredentials";

        public async Task<ImportResultItem> ImportAsync(ConnectionEntry entry, ImportSession session, CancellationToken cancellationToken)
        {
            var type = ConfigurationEntryTypes.Connection;
            var key = Guid.Parse(entry.Key!);
            var normalizedKey = key.ToString("D");
            var workspaceId = Guid.Parse(entry.WorkspaceId!);
            var tenantId = Guid.Parse(entry.TenantId!);
            var clientId = Guid.Parse(entry.ClientId!);
            var name = entry.Name!.Trim();
            var environment = string.IsNullOrWhiteSpace(entry.Environment) ? null : entry.Environment.Trim();

            var repository = unitOfWork.ConnectionRepository;
            var existing = await repository.FindByExternalKeyAsync(key, true, cancellationToken)
                ?? await repository.FindByWorkspaceIdAsync(workspaceId, true, cancellationToken);

            var action = session.Mode.Decide(existing is not null);

            if (action == ImportAction.Skip)
            {
                session.References.Known[normalizedKey] = existing!.Id;
                return ImportResultItem.Succeeded(type, normalizedKey, name, ImportResultItem.Skipped, existing.Id, []);
            }

            var now = clock.GetUtcNow();
            ConnectionEntity connection;

            if (action == ImportAction.Create)
            {
                connection = new ConnectionEntity
                {
                    Id = Guid.NewGuid(),
                    ExternalKey = key,
                    Name = name,
                    Environment = environment,
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ClientId = clientId,
                    ClientSecret = string.Empty,
                    CreatedAt = now,
                    CreatedBy = session.UserId
                };
                await repository.AddAsync(connection, cancellationToken);
            }
            else
            {
                connection = existing!;

                var clash = await repository.FindByWorkspaceIdAsync(workspaceId, false, cancellationToken);
                if (clash is not null && clash.Id != connection.Id)
                    return ImportResultItem.Failure(type, normalizedKey, name, ErrorCodes.Connection.Duplicated, "Workspace id is used by another connection.");

                // A secret only works with the client and tenant it was created for.
                if (connection.ClientId != clientId || connection.TenantId != tenantId)
                    connection.ClientSecret = string.Empty;

                connection.ExternalKey = key;
                connection.Name = name;
                connection.Environment = environment;
                connection.TenantId = tenantId;
                connection.WorkspaceId = workspaceId;
                connection.ClientId = clientId;
                connection.UpdatedAt = now;
                connection.UpdatedBy = session.UserId;
                repository.Update(connection);
            }

            var flags = new List<string>();
            foreach (var step in steps)
                flags.AddRange(await step.ApplyAsync(connection, entry, session, cancellationToken));

            await unitOfWork.SaveChangesAsync(cancellationToken);

            session.References.Known[normalizedKey] = connection.Id;

            if (connection.CredentialStatus == CredentialStatuses.Missing)
                flags.Add(NeedsCredentials);

            return ImportResultItem.Succeeded(
                type, normalizedKey, name,
                action == ImportAction.Create ? ImportResultItem.Created : ImportResultItem.Updated,
                connection.Id, flags);
        }
    }
}
