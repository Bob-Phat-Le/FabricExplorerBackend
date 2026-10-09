namespace FabricExplorerBackend.Features.Token
{
    public interface ITokenService
    {
        Task<string> CreateTokenAsync(Domain.Entities.Connection connection, string scope, CancellationToken cancellationToken = default);
        Task<string> GetTokenAsync(Domain.Entities.Connection connection, string scope, CancellationToken cancellationToken = default);
        Task<string> GetOrCreateTokenAsync(Domain.Entities.Connection connection, string scope, CancellationToken cancellationToken = default);
    }
}
