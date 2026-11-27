using FabricExplorerBackend.Application.Configuration.Contracts;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;

namespace FabricExplorerBackend.Application.Configuration.Import
{
    /// <summary>One transaction for everything: a single problem rolls every entry back.</summary>
    public sealed class AtomicExecutionStrategy(IUnitOfWork unitOfWork, ILogger<AtomicExecutionStrategy> logger) : IImportExecutionStrategy
    {
        public bool Atomic => true;

        public async Task<IReadOnlyList<ImportResultItem>> ExecuteAsync(
            IReadOnlyList<ImportUnit> units, ImportSession session, CancellationToken cancellationToken)
        {
            var results = new List<ImportResultItem>(units.Count);

            await using var transaction = await unitOfWork.StartTransaction(cancellationToken);
            try
            {
                foreach (var unit in units)
                {
                    var result = await unit.ApplyAsync(session, cancellationToken);
                    results.Add(result);

                    if (!result.Success)
                        break;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Atomic import failed");
                results.Add(ImportResultItem.Failure(
                    ConfigurationEntryTypes.Connection, null, null, ErrorCodes.Configuration.EntryFailed, "An entry could not be saved."));
            }

            if (results.Count == units.Count && results.All(r => r.Success))
            {
                await unitOfWork.CommitTransaction(cancellationToken);
                return results;
            }

            await unitOfWork.RollbackTransaction(cancellationToken);
            unitOfWork.ClearTracking();

            // Nothing was saved: the entries that did not fail are reported as rolled back.
            return results
                .Select(r => r.Success
                    ? new ImportResultItem(r.Type, r.Key, r.Name, ImportResultItem.Skipped, null, false,
                        new ImportItemError(ErrorCodes.Configuration.RolledBack, "Rolled back because another entry failed."), [])
                    : r)
                .Concat(units
                    .Skip(results.Count)
                    .Select(u => new ImportResultItem(u.Type, u.Key, u.Name, ImportResultItem.Skipped, null, false,
                        new ImportItemError(ErrorCodes.Configuration.RolledBack, "Not imported because another entry failed."), [])))
                .ToList();
        }
    }
}
