using FabricExplorerBackend.Infrastructures.Sql;

namespace FabricExplorerBackend.Application.QueryService.Contracts
{
    /// <param name="MaxRows">
    /// Optional record limit. Null (or &lt;= 0) = "Unlimited": no artificial limit while executing and storing
    /// the result. It never means "return everything in one response" - the result is always read in pages.
    /// </param>
    public sealed record QueryRequest(Guid ConnectionId, SqlEndpoint Endpoint, string Query, long? MaxRows = null);
}
