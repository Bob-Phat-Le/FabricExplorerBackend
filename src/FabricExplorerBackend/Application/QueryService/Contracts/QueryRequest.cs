using FabricExplorerBackend.Infrastructures.Sql;

namespace FabricExplorerBackend.Application.QueryService.Contracts
{
    public sealed record QueryRequest(Guid ConnectionId, SqlEndpoint Endpoint, string Query);
}
