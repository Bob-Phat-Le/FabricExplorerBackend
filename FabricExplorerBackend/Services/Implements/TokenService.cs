using Azure.Core;
using Azure.Identity;
using FabricExplorerBackend.Securities;
using FabricExplorerBackend.Services.Interfaces;

namespace FabricExplorerBackend.Services.Implements
{
    public class TokenService(ICacheService cacheService) : ITokenService
    {
        public async Task<string> CreateTokenAsync(Guid tenantId, Guid clientId, string secret)
        {
            var credential = new ClientSecretCredential(
                tenantId.ToString(),
                clientId.ToString(),
                secret
            );

            var token = await credential.GetTokenAsync(new TokenRequestContext(new[] { "https://api.fabric.microsoft.com/.default" }));
            return token.Token;
        }

        public async Task<string> GetTokenAsync(Guid userId, Guid connectionId)
        {
            var key = cacheService.CreateCacheKey(userId.ToString(), connectionId.ToString());
            var result = await cacheService.GetValueAsync(key);
            return result?.ToString() ?? string.Empty;
        }
    }
}
