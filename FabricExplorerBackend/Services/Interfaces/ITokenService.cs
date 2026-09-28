namespace FabricExplorerBackend.Services.Interfaces
{
    public interface ITokenService
    {
        Task<string> CreateTokenAsync(Guid tenantId, Guid connectionId, string secret);
        Task<string> GetTokenAsync(Guid userId, Guid connectionId);
    }
}
