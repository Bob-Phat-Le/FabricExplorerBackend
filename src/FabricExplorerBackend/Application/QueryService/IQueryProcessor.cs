namespace FabricExplorerBackend.Application.QueryService
{
    /// <summary>Drives one query through Pending -> Running -> Completed/Failed/Cancelled.</summary>
    public interface IQueryProcessor
    {
        Task RunAsync(QueryJob job, CancellationToken stoppingToken);
    }
}
