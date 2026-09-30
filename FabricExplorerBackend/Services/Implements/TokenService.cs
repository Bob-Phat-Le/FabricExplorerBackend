using Azure.Core;
using Azure.Identity;
using FabricExplorerBackend.Securities;
using FabricExplorerBackend.Services.Interfaces;

namespace FabricExplorerBackend.Services.Implements
{
    public class TokenService(ICacheService cacheService) : ITokenService
    {
        public async Task<string> CreateTokenAsync(Guid tenantId, Guid clientId, Guid workspaceId, string secret)
        {
            var credential = new ClientSecretCredential(
                tenantId.ToString(),
                clientId.ToString(),
                secret
            );

            var token = await credential.GetTokenAsync(new TokenRequestContext(new[] { "https://api.fabric.microsoft.com/.default" }));

            var key = cacheService.CreateCacheKey(tenantId.ToString(), clientId.ToString(), workspaceId.ToString());
            await cacheService.SetValueAsync(key, token.Token, token.ExpiresOn);

            return token.Token;
        }

        public async Task<string> GetTokenAsync(Guid tenantId, Guid clientId, Guid workspaceId)
        {
            var key = cacheService.CreateCacheKey(tenantId.ToString(), clientId.ToString(), workspaceId.ToString());
            var result = await cacheService.GetValueAsync(key);
            return result?.ToString() ?? string.Empty;
        }
    }
}
