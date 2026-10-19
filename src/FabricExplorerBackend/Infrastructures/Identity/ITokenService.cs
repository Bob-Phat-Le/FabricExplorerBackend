using Azure.Identity;

namespace FabricExplorerBackend.Infrastructures.Identity
{
    public interface ITokenService
    {
        Task<string> CreateTokenAsync(Domain.Entities.Connection connection, string scope, CancellationToken cancellationToken = default);
        Task<string> GetTokenAsync(Domain.Entities.Connection connection, string scope, CancellationToken cancellationToken = default);
        Task<string> GetOrCreateTokenAsync(Domain.Entities.Connection connection, string scope, CancellationToken cancellationToken = default);
        ClientSecretCredential CreateClientSecretCredential(Guid tenantId, Guid clientId, string clientSecret);
    }
}
