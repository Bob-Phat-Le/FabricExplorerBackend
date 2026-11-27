using FabricExplorerBackend.Application.Configuration.Contracts;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;

namespace FabricExplorerBackend.Application.Configuration.Import
{
    /// <summary>Each unit saves on its own; a failure is reported for that entry and the rest continue.</summary>
    public sealed class BestEffortExecutionStrategy(IUnitOfWork unitOfWork, ILogger<BestEffortExecutionStrategy> logger) : IImportExecutionStrategy
    {
        public bool Atomic => false;

        public async Task<IReadOnlyList<ImportResultItem>> ExecuteAsync(
            IReadOnlyList<ImportUnit> units, ImportSession session, CancellationToken cancellationToken)
        {
            var results = new List<ImportResultItem>(units.Count);

            foreach (var unit in units)
            {
                try
                {
                    results.Add(await unit.ApplyAsync(session, cancellationToken));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Importing {Type} '{Key}' failed", unit.Type, unit.Key);
                    unitOfWork.ClearTracking();
                    results.Add(ImportResultItem.Failure(
                        unit.Type, unit.Key, unit.Name, ErrorCodes.Configuration.EntryFailed, "The entry could not be saved."));
                }
            }

            return results;
        }
    }
}
