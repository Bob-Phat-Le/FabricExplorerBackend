namespace FabricExplorerBackend.Infrastructures.BackgroundJobs
{
    /// <summary>Removes the results (and metadata) of queries whose <c>ExpiresAt</c> has passed.</summary>
    public interface IQueryResultCleanupService
    {
        /// <returns>The number of expired queries that were removed.</returns>
        Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default);
    }
}
