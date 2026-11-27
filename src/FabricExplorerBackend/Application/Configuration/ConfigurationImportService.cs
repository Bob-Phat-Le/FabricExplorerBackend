using FabricExplorerBackend.Application.Configuration.Contracts;
using FabricExplorerBackend.Application.Configuration.Documents;
using FabricExplorerBackend.Application.Configuration.Handlers;
using FabricExplorerBackend.Application.Configuration.Import;
using FabricExplorerBackend.Application.Configuration.Validation;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Commons.Security;
using FabricExplorerBackend.Domain.Entities;
using FluentValidation;

namespace FabricExplorerBackend.Application.Configuration
{
    public sealed class ConfigurationImportService(
        IValidator<ImportRequest> requestValidator,
        IValidator<ImportIdempotencyKey> idempotencyKeyValidator,
        IConfigurationDocumentReader reader,
        IIntegrityHasher hasher,
        IConfigurationHandlerRegistry registry,
        IConnectionReferenceBuilder referenceBuilder,
        IEnumerable<IImportModeStrategy> modes,
        IEnumerable<IImportExecutionStrategy> executionStrategies,
        IConfigurationHistoryService history,
        ICurrentUser currentUser) : IConfigurationImportService
    {
        public async Task<Result<PreviewResponse>> PreviewAsync(PreviewRequest request, CancellationToken cancellationToken = default)
        {
            var read = reader.Read(request.Document);
            if (!read.IsSuccess)
                return Result<PreviewResponse>.Success(new PreviewResponse(
                    null, true,
                    read.Errors.Select(e => new FileIssue(e.Code, e.Message)).ToList(),
                    [],
                    new PreviewSummary(0, 0, 0, 0)));

            var document = read.Data!;

            var references = await referenceBuilder.BuildAsync(document, null, cancellationToken);
            if (!references.IsSuccess)
                return Result<PreviewResponse>.Failure(references.Errors.ToArray());

            var context = new PreviewContext
            {
                UserId = currentUser.Id,
                Document = document,
                References = references.Data!
            };

            var entries = new List<PreviewEntry>();
            foreach (var handler in registry.Ascending)
                entries.AddRange(await handler.PreviewAsync(context, cancellationToken));

            var summary = new PreviewSummary(
                entries.Count,
                entries.Count(e => e.Validation.Valid && e.Match.Kind == MatchInfo.None),
                entries.Count(e => e.Validation.Valid && e.Match.Kind != MatchInfo.None),
                entries.Count(e => !e.Validation.Valid));

            return Result<PreviewResponse>.Success(new PreviewResponse(
                document.FormatVersion, hasher.Verify(document), [], entries, summary));
        }

        public async Task<Result<ImportResponse>> ImportAsync(ImportRequest request, string? idempotencyKey, CancellationToken cancellationToken = default)
        {
            var userId = currentUser.Id;

            var key = ImportIdempotencyKey.From(idempotencyKey);
            var keyValidation = idempotencyKeyValidator.Validate(key);
            if (!keyValidation.IsValid)
                return keyValidation.ToBadRequest();

            // if the key is provided, check if there is a previous import operation with the same key for this user; if so, return the previous result
            if (key.Value is not null)
            {
                var replay = await history.FindImportReplayAsync(userId, key.Value, cancellationToken);
                if (replay is not null)
                    return Result<ImportResponse>.Success(replay);
            }

            // mode and selection are checked here, so everything below can rely on them being valid
            var requestValidation = requestValidator.Validate(request);
            if (!requestValidation.IsValid)
                return requestValidation.ToBadRequest();

            // deserialize the document from request.
            var read = reader.Read(request.Document);
            if (!read.IsSuccess)
                return Result<ImportResponse>.Failure(read.Errors.ToArray());

            var document = read.Data!;
            var options = request.Options ?? new ImportOptions();

            if (!options.IgnoreIntegrityMismatch && !hasher.Verify(document))
                return Error.BadRequest(ErrorCodes.Configuration.IntegrityMismatch,
                    "The file was changed after it was exported. Set options.ignoreIntegrityMismatch to import it anyway.");

            var modeName = string.IsNullOrWhiteSpace(request.Mode) ? "merge" : request.Mode.Trim();
            var mode = modes.First(m => string.Equals(m.Name, modeName, StringComparison.OrdinalIgnoreCase));

            var selection = BuildSelection(request.Selection);

            // this operation is to map the document to a list of ImportUnit (include known and unknown entries)
            // the reason we also need to map the known entries is that SaveQueries (for example) may have references to an known entry (like a connection) that is not selected for import, but we still need to check if the reference is valid; if the reference is invalid, we should return an error
            var references = await referenceBuilder.BuildAsync(document, options.ReferenceMappings, cancellationToken);
            if (!references.IsSuccess)
                return Result<ImportResponse>.Failure(references.Errors.ToArray());
            
            var session = new ImportSession
            {
                UserId = userId,
                Document = document,
                Options = options,
                Mode = mode,
                References = references.Data!,
                Selection = selection
            };

            var units = new List<ImportUnit>();
            foreach (var handler in registry.Ascending)
                units.AddRange(await handler.PlanImportAsync(session, cancellationToken));

            var strategy = executionStrategies.First(s => s.Atomic == options.Atomic);
            var results = await strategy.ExecuteAsync(units, session, cancellationToken);

            var response = new ImportResponse(
                results.Count(r => r.Action == ImportResultItem.Created),
                results.Count(r => r.Action == ImportResultItem.Updated),
                results.Count(r => r.Action == ImportResultItem.Skipped),
                results.Count(r => r.Action == ImportResultItem.Failed),
                results);

            var status = response.Failed > 0 || results.Any(r => !r.Success)
                ? (response.Created + response.Updated > 0 ? ConfigurationOperationStatuses.CompletedWithErrors : ConfigurationOperationStatuses.Failed)
                : ConfigurationOperationStatuses.Completed;

            await history.RecordAsync(
                userId, ConfigurationOperationKinds.Import, status,
                $"Import ({mode.Name}{(options.Atomic ? ", atomic" : string.Empty)}): {response.Created} created, {response.Updated} updated, {response.Skipped} skipped, {response.Failed} failed.",
                results.Count, key.Value, response, cancellationToken);

            return Result<ImportResponse>.Success(response);
        }

        /// <summary>"Type|key" of every chosen entry; null (= every entry of the file) when nothing was chosen. The refs were checked by ImportRequestValidator.</summary>
        private HashSet<string>? BuildSelection(List<ImportEntryRef>? refs)
        {
            if (refs is not { Count: > 0 })
                return null;

            var selection = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in refs)
            {
                // well, we can just add the item to the selection, but we should also check if the item actually exists in the document; if not, we should return an error
                if (registry.TryGet(item.Type, out var handler) && ConfigurationGuid.TryParse(item.Key, out var itemKey))
                    selection.Add($"{handler.Type}|{itemKey:D}");
            }

            return selection;
        }
    }
}
