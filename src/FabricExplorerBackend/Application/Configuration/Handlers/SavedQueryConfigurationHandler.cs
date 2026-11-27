using FabricExplorerBackend.Application.Configuration.Contracts;
using FabricExplorerBackend.Application.Configuration.Documents;
using FabricExplorerBackend.Application.Configuration.Export;
using FabricExplorerBackend.Application.Configuration.Import;
using FabricExplorerBackend.Application.Configuration.Validation;
using FabricExplorerBackend.Application.SavedQueries;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using FluentValidation;

namespace FabricExplorerBackend.Application.Configuration.Handlers
{
    public sealed class SavedQueryConfigurationHandler(
        IUnitOfWork unitOfWork,
        IValidator<SavedQueryEntry> validator,
        ISavedQueryMapper mapper,
        ISavedQueryImporter importer) : IConfigurationEntryHandler
    {
        public const string ConnectionResolved = "Resolved";
        public const string ConnectionUnresolved = "Unresolved";

        public string Type => ConfigurationEntryTypes.SavedQuery;
        public string Collection => ConfigurationDocumentBuilder.SavedQueriesCollection;
        public int Order => 20;

        public async Task<IReadOnlyList<ConfigurationItemResponse>> ListInventoryAsync(Guid userId, CancellationToken cancellationToken)
        {
            var queries = await unitOfWork.SavedQueryRepository.ListByOwnerAsync(userId, cancellationToken);
            var (connections, _) = await unitOfWork.ConnectionRepository.GetAllAsync(0, 0, cancellationToken: cancellationToken);
            var names = connections.ToDictionary(c => c.Id, c => c.Name);

            return queries
                .Select(q =>
                {
                    var resolved = names.TryGetValue(q.ConnectionId, out var connectionName);
                    return new ConfigurationItemResponse(
                        Type, q.Id, q.ExternalKey, q.Name, $"{q.ItemType}: {q.ItemName}", connectionName,
                        q.UpdatedAt ?? q.CreatedAt,
                        resolved ? ConnectionResolved : ConnectionUnresolved,
                        resolved ? Array.Empty<string>() : new[] { "connection_unresolved" });
                })
                .ToList();
        }

        public async Task CollectExportAsync(ExportContext context, CancellationToken cancellationToken)
        {
            var queries = (await unitOfWork.SavedQueryRepository.ListByOwnerAsync(context.UserId, cancellationToken))
                .Where(q => context.IsSelected(Type, q.Id))
                .ToList();

            var connections = await unitOfWork.ConnectionRepository.GetByIdsAsync(
                queries.Select(q => q.ConnectionId).Distinct().ToList(), cancellationToken: cancellationToken);
            var byId = connections.ToDictionary(c => c.Id);

            foreach (var query in queries)
            {
                if (!byId.TryGetValue(query.ConnectionId, out var connection))
                {
                    context.Skipped++;
                    continue;
                }

                context.RequiredConnectionIds.Add(connection.Id);
                context.Builder.AddSavedQuery(mapper.ToEntry(query, connection.ExternalKey));
            }
        }

        public async Task<IReadOnlyList<PreviewEntry>> PreviewAsync(PreviewContext context, CancellationToken cancellationToken)
        {
            var existing = await unitOfWork.SavedQueryRepository.ListByOwnerAsync(context.UserId, cancellationToken);
            var byKey = existing.ToDictionary(q => q.ExternalKey);
            var byName = existing.ToDictionary(q => (q.ConnectionId, q.Name));

            var entries = new List<PreviewEntry>();

            foreach (var entry in context.Document.SavedQueries ?? [])
            {
                // The connection key rule only checks the shape; whether it points anywhere is decided here.
                var issues = validator
                    .Validate(entry)
                    .ToIssues()
                    .Where(i => i.Field != "connectionKey")
                    .ToList();

                var state = context.References.Resolve(entry.ConnectionKey, out var connectionId);
                if (state == ReferenceState.Unresolved)
                    issues.Add(new ValidationIssue("reference_unresolved", "connectionKey", "Connection not in file or system"));

                var match = MatchInfo.NoMatch;
                if (ConfigurationGuid.TryParse(entry.Key, out var key) && byKey.TryGetValue(key, out var exact))
                    match = new MatchInfo(MatchInfo.Exact, exact.Id, "key");
                else if (state == ReferenceState.Existing && entry.Name is not null && byName.TryGetValue((connectionId, entry.Name.Trim()), out var probable))
                    match = new MatchInfo(MatchInfo.Probable, probable.Id, "name");

                entries.Add(new PreviewEntry(Type, entry.Key, entry.Name, match, new ValidationResult(issues.Count == 0, issues), []));
            }

            return entries;
        }

        public Task<IReadOnlyList<ImportUnit>> PlanImportAsync(ImportSession session, CancellationToken cancellationToken)
        {
            var units = new List<ImportUnit>();

            foreach (var entry in session.Document.SavedQueries ?? [])
            {
                if (!session.IsSelected(Type, entry.Key))
                    continue;

                // An unresolved connection is reported as a skip by the importer, not as invalid content.
                var issues = validator
                    .Validate(entry)
                    .ToIssues()
                    .Where(i => i.Field != "connectionKey")
                    .ToList();

                units.Add(issues.Count > 0
                    ? ImportUnit.Rejected(Type, entry.Key, entry.Name, issues[0])
                    : new ImportUnit(Type, entry.Key, entry.Name, (s, token) => importer.ImportAsync(entry, s, token)));
            }

            return Task.FromResult<IReadOnlyList<ImportUnit>>(units);
        }
    }
}
