using Azure.Core;
using Azure.Identity;
using FabricExplorerBackend.Features.Cache;
using FabricExplorerBackend.Infrastructures.Securities;

namespace FabricExplorerBackend.Features.Token
{
    public class TokenService(
        ICacheService cacheService,
        ISecretProtector secretProtector) : ITokenService
    {
        public async Task<string> CreateTokenAsync(Domain.Entities.Connection connection)
        {
            var credential = new ClientSecretCredential(
                connection.TenantId.ToString(),
                connection.ClientId.ToString(),
                secretProtector.Unprotect(connection.ClientSecret)
            );

            var token = await credential.GetTokenAsync(new TokenRequestContext(new[] { "https://api.fabric.microsoft.com/.default" }));

            var key = cacheService.CreateCacheKey(connection.TenantId.ToString(), connection.ClientId.ToString(), connection.WorkspaceId.ToString());
            await cacheService.SetValueAsync(key, token.Token, token.ExpiresOn - DateTimeOffset.UtcNow);

            return token.Token;
        }

        public async Task<string> GetOrCreateTokenAsync(Domain.Entities.Connection connection)
        {
            var key = cacheService.CreateCacheKey(connection.TenantId.ToString(), connection.ClientId.ToString(), connection.WorkspaceId.ToString());
            var result = await cacheService.GetValueAsync(key);
            if (string.IsNullOrEmpty(result?.ToString()))
                return await CreateTokenAsync(connection);
            return result.ToString()!;
        }

        public async Task<string> GetTokenAsync(Domain.Entities.Connection connection)
        {
            var key = cacheService.CreateCacheKey(connection.TenantId.ToString(), connection.ClientId.ToString(), connection.WorkspaceId.ToString());
            var result = await cacheService.GetValueAsync(key);
            return result?.ToString() ?? string.Empty;
        }
    }
}
