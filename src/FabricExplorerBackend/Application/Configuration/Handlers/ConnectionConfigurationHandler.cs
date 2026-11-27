using FabricExplorerBackend.Application.Configuration.Contracts;
using FabricExplorerBackend.Application.Configuration.Documents;
using FabricExplorerBackend.Application.Configuration.Export;
using FabricExplorerBackend.Application.Configuration.Import;
using FabricExplorerBackend.Application.Configuration.Validation;
using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using FluentValidation;
using ConnectionEntity = FabricExplorerBackend.Domain.Entities.Connection;

namespace FabricExplorerBackend.Application.Configuration.Handlers
{
    public sealed class ConnectionConfigurationHandler(
        IUnitOfWork unitOfWork,
        IValidator<ConnectionEntry> validator,
        IEnumerable<IConnectionExportContributor> exportContributors,
        IConnectionImporter importer) : IConfigurationEntryHandler
    {
        public string Type => ConfigurationEntryTypes.Connection;
        public string Collection => ConfigurationDocumentBuilder.ConnectionsCollection;
        public int Order => 10;

        public async Task<IReadOnlyList<ConfigurationItemResponse>> ListInventoryAsync(Guid userId, CancellationToken cancellationToken)
        {
            var (connections, _) = await unitOfWork.ConnectionRepository.GetAllAsync(0, 0, cancellationToken: cancellationToken);

            return connections
                .Select(c => new ConfigurationItemResponse(
                    Type, c.Id, c.ExternalKey, c.Name, c.WorkspaceId.ToString("D"), null,
                    c.UpdatedAt ?? c.CreatedAt, c.CredentialStatus,
                    c.CredentialStatus == CredentialStatuses.Missing ? new[] { "credentials_missing" } : Array.Empty<string>()))
                .ToList();
        }

        public async Task CollectExportAsync(ExportContext context, CancellationToken cancellationToken)
        {
            var (connections, _) = await unitOfWork.ConnectionRepository.GetAllAsync(0, 0, cancellationToken: cancellationToken);

            // Whitelist: only the fields written below can ever reach the file (never the secret).
            foreach (var connection in connections.Where(c => context.IsSelected(Type, c.Id) || context.RequiredConnectionIds.Contains(c.Id)))
            {
                var entry = new ConnectionEntry
                {
                    Key = connection.ExternalKey.ToString("D"),
                    Name = connection.Name,
                    Environment = connection.Environment,
                    WorkspaceId = connection.WorkspaceId.ToString("D"),
                    WorkspaceName = null,
                    TenantId = connection.TenantId.ToString("D"),
                    TokenEndpoint = TokenEndpoints.ForTenant(connection.TenantId),
                    ClientId = connection.ClientId.ToString("D"),
                    Credentials = new ConnectionCredentials { ClientSecret = ConfigurationFormat.SecretExcluded },
                    LandingZone = null,
                    CreatedAt = connection.CreatedAt,
                    UpdatedAt = connection.UpdatedAt
                };

                foreach (var contributor in exportContributors)
                    await contributor.ContributeAsync(connection, entry, context, cancellationToken);

                context.Builder.AddConnection(entry);
            }
        }

        public async Task<IReadOnlyList<PreviewEntry>> PreviewAsync(PreviewContext context, CancellationToken cancellationToken)
        {
            var (existing, _) = await unitOfWork.ConnectionRepository.GetAllAsync(0, 0, cancellationToken: cancellationToken);
            var byKey = existing.ToDictionary(c => c.ExternalKey);
            var byWorkspace = existing.ToDictionary(c => c.WorkspaceId);

            var entries = new List<PreviewEntry>();

            foreach (var entry in context.Document.Connections ?? [])
            {
                var issues = validator.Validate(entry).ToIssues();
                var match = MatchInfo.NoMatch;
                ConnectionEntity? matched = null;

                if (ConfigurationGuid.TryParse(entry.Key, out var key) && byKey.TryGetValue(key, out var exact))
                {
                    matched = exact;
                    match = new MatchInfo(MatchInfo.Exact, exact.Id, "key");
                }
                else if (ConfigurationGuid.TryParse(entry.WorkspaceId, out var workspaceId) && byWorkspace.TryGetValue(workspaceId, out var probable))
                {
                    matched = probable;
                    match = new MatchInfo(MatchInfo.Probable, probable.Id, "workspaceId");
                }

                var flags = new List<string>();
                if (matched is null)
                    flags.Add(ConnectionImporter.NeedsCredentials);
                else if (ConfigurationGuid.TryParse(entry.ClientId, out var clientId)
                         && ConfigurationGuid.TryParse(entry.TenantId, out var tenantId)
                         && (matched.ClientId != clientId || matched.TenantId != tenantId))
                    flags.Add("CredentialsResetOnOverwrite");

                entries.Add(new PreviewEntry(Type, entry.Key, entry.Name, match, new ValidationResult(issues.Count == 0, issues), flags));
            }

            return entries;
        }

        public Task<IReadOnlyList<ImportUnit>> PlanImportAsync(ImportSession session, CancellationToken cancellationToken)
        {
            var units = new List<ImportUnit>();

            foreach (var entry in session.Document.Connections ?? [])
            {
                if (!session.IsSelected(Type, entry.Key))
                    continue;

                var issues = validator.Validate(entry).ToIssues();
                units.Add(issues.Count > 0
                    ? ImportUnit.Rejected(Type, entry.Key, entry.Name, issues[0])
                    : new ImportUnit(Type, entry.Key, entry.Name, (s, token) => importer.ImportAsync(entry, s, token)));
            }

            return Task.FromResult<IReadOnlyList<ImportUnit>>(units);
        }
    }
}
