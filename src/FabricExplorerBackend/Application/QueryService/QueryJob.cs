using FabricExplorerBackend.Infrastructures.Sql;

namespace FabricExplorerBackend.Application.QueryService
{
    /// <summary>Unit of work handed from the API to the background workers.</summary>
    public sealed record QueryJob(Guid QueryId, Guid UserId, Guid ConnectionId, SqlEndpoint Endpoint, long? MaxRows);
}
