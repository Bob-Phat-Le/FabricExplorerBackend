using FabricExplorerBackend.Application.Configuration.Contracts;
using FabricExplorerBackend.Application.Configuration.Documents;
using FabricExplorerBackend.Application.Configuration.Export;
using FabricExplorerBackend.Application.Configuration.Handlers;
using FabricExplorerBackend.Application.Configuration.Validation;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Commons.Security;
using FabricExplorerBackend.Domain.Entities;
using FluentValidation;
using System.Text.Json;

namespace FabricExplorerBackend.Application.Configuration
{
    public sealed class ConfigurationExportService(
        IValidator<ExportRequest> requestValidator,
        IConfigurationHandlerRegistry registry,
        IConfigurationDocumentBuilder builder,
        IConfigurationHistoryService history,
        ICurrentUser currentUser,
        TimeProvider clock) : IConfigurationExportService
    {
        // how this method works:
        // 1. validate the request (scope, items) with ExportRequestValidator
        // 2. create an ExportContext with the current user, scope, include, and builder
        // 3. if scope is selected, add the (already validated) items to the context.Selected dictionary
        // 4. loop through the handlers in descending order and call CollectExportAsync on each handler, passing the context and cancellation token
        // 5. after all handlers have collected their data, check if any items were exported; if none, return NotFound
        // 6. build the final ConfigurationDocument using the builder
        // 7. serialize the document to JSON using the File options
        // 8. create a file name based on the current UTC time
        // 9. record the export operation in the history service

        public async Task<Result<ConfigurationFile>> ExportAsync(ExportRequest request, CancellationToken cancellationToken = default)
        {
            var validation = requestValidator.Validate(request);
            if (!validation.IsValid)
                return validation.ToBadRequest();

            var scope = ConfigurationFormat.NormalizeScope(request.Scope);

            var userId = currentUser.Id;
            var context = new ExportContext
            {
                UserId = userId,
                ScopeAll = scope == ConfigurationFormat.ScopeAll,
                Include = request.Include ?? new ExportInclude(),
                Builder = builder
            };

            if (!context.ScopeAll)
            {
                foreach (var item in request.Items!)
                {
                    // every item was checked by ExportRequestValidator, so the type is known
                    if (!registry.TryGet(item.Type, out var handler))
                        continue;

                    // if context.Selected.TryGetValue returns false, so this is the first time to add item.Id to HashSet<Guid> for this type, we need to create a new HashSet<Guid> and add it to context.Selected
                    if (!context.Selected.TryGetValue(handler.Type, out var ids))
                    {
                        ids = new HashSet<Guid>();
                        context.Selected[handler.Type] = ids;
                    }
                    ids.Add(item.Id);
                }
            }

            // this is the main loop that calls each handler to collect the export data
            // although the handlers use the context.Builder to add their data, this separation is because we might have more type of item to be exported in the future, so we need to have a common builder to collect all the data from different handlers; the context.Builder itself shouldn't take responsibility for the logic of how to collect the data (and map it to the final entry), that's the responsibility of each handler
            foreach (var handler in registry.Descending)
                await handler.CollectExportAsync(context, cancellationToken);

            var exported = builder.ConnectionCount + builder.SavedQueryCount;
            if (!context.ScopeAll && exported == 0)
                return Error.NotFound(ErrorCodes.Configuration.InvalidSelection, "None of the selected items exist.");

            var document = builder.Build(scope);
            var content = JsonSerializer.SerializeToUtf8Bytes(document, ConfigurationJson.File);
            var fileName = $"fabric-explorer-configuration-{clock.GetUtcNow():yyyyMMdd-HHmm}.json";

            var summary = $"Exported {builder.ConnectionCount} connection(s) and {builder.SavedQueryCount} saved query(ies)"
                + (context.Include.LandingZone ? " with Landing Zone." : ".");
            await history.RecordAsync(
                userId, ConfigurationOperationKinds.Export, ConfigurationOperationStatuses.Completed,
                summary, exported, null, null, cancellationToken);

            return Result<ConfigurationFile>.Success(
                new ConfigurationFile(content, fileName, "application/json", exported, context.Skipped));
        }
    }
}
