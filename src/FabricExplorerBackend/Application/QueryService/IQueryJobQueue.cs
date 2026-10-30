namespace FabricExplorerBackend.Application.QueryService
{
    /// <summary>
    /// In-memory hand-off between <c>POST /queries</c> and the background workers, plus the registry of
    /// running queries so they can be cancelled.
    /// </summary>
    public interface IQueryJobQueue
    {
        /// <summary>False when the queue is full.</summary>
        bool TryEnqueue(QueryJob job);

        IAsyncEnumerable<QueryJob> DequeueAsync(CancellationToken cancellationToken);

        /// <summary>Registers a run and returns the token source that <see cref="TryCancel"/> triggers.</summary>
        CancellationTokenSource BeginRun(Guid queryId, CancellationToken stoppingToken);

        /// <summary>Signals a running query to stop. False when it is not running.</summary>
        bool TryCancel(Guid queryId);

        void EndRun(Guid queryId);
    }
}
