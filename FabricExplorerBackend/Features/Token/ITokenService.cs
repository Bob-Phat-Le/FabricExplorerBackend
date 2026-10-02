namespace FabricExplorerBackend.Features.Token
{
    public interface ITokenService
    {
        Task<string> CreateTokenAsync(Guid tenantId, Guid clientId, Guid workspaceId, string secret);
        Task<string> GetTokenAsync(Guid tenantId, Guid clientId, Guid workspaceId);
    }
}
