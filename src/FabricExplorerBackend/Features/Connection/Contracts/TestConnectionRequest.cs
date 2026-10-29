namespace FabricExplorerBackend.Features.Connection.Contracts
{
    public sealed record TestConnectionRequest(Guid TenantId, Guid ClientId, string ClientSecret, Guid WorkspaceId);
}
