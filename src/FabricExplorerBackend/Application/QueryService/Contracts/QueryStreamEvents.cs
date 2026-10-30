using FabricExplorerBackend.Commons.EventStreaming;
using FabricExplorerBackend.Domain.Enums;

namespace FabricExplorerBackend.Application.QueryService.Contracts
{
    /// <summary>
    /// SSE contract of a query. One stream per query (channel "query:{id}"), lifecycle only - never rows.
    /// <list type="bullet">
    /// <item><c>status</c>: Pending / Running (non-terminal)</item>
    /// <item><c>completed</c>: terminal, Completed or Cancelled (check the status in the payload)</item>
    /// <item><c>error</c>: terminal, Failed (payload carries the error)</item>
    /// </list>
    /// </summary>
    public static class QueryStreamEvents
    {
        public const string ChannelPrefix = "query:";
        public const string Status = "status";

        public static string ChannelFor(Guid queryId) => ChannelPrefix + queryId.ToString("D");

        public static string EventNameFor(QueryStatus status) => status switch
        {
            QueryStatus.Failed => StreamEventNames.Error,
            QueryStatus.Completed or QueryStatus.Cancelled => StreamEventNames.Completed,
            _ => Status
        };
    }
}
