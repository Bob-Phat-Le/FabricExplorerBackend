namespace FabricExplorerBackend.Features.Token
{
    public interface ITokenService
    {
        Task<string> CreateTokenAsync(Domain.Entities.Connection connection);
        Task<string> GetTokenAsync(Domain.Entities.Connection connection);
        Task<string> GetOrCreateTokenAsync(Domain.Entities.Connection connection);
    }
}
