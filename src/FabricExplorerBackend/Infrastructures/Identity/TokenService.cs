using Azure.Core;
using Azure.Identity;
using FabricExplorerBackend.Infrastructures.Cache;
using FabricExplorerBackend.Infrastructures.Options;
using FabricExplorerBackend.Infrastructures.Security;
using Microsoft.Extensions.Options;

namespace FabricExplorerBackend.Infrastructures.Identity
{
    public class TokenService(
        ICacheService cacheService,
        ISecretProtector secretProtector,
        ISingleFlight singleFlight,
        IOptions<AuthenticationOptions> authenOptions) : ITokenService
    {
        private AuthenticationOptions authenOptions = authenOptions.Value;

        public ClientSecretCredential CreateClientSecretCredential(Guid tenantId, Guid clientId, string clientSecret)
            => new ClientSecretCredential(
                tenantId.ToString(),
                clientId.ToString(),
                secretProtector.Unprotect(clientSecret)
            );

        public Task<string> CreateTokenAsync(Domain.Entities.Connection connection, string scope, CancellationToken cancellationToken = default)
            => RequestAndCacheTokenAsync(connection, scope, cancellationToken);

        public async Task<string> GetOrCreateTokenAsync(Domain.Entities.Connection connection, string scope, CancellationToken cancellationToken = default)
        {
            var key = cacheService.CreateCacheKey(connection.TenantId.ToString(), connection.ClientId.ToString(), scope);
            var cached = (await cacheService.GetValueAsync(key))?.ToString();
            if (!string.IsNullOrEmpty(cached))
                return cached;

            return await singleFlight.RunAsync($"token:{key}", async () =>
            {
                var again = (await cacheService.GetValueAsync(key))?.ToString();
                if (!string.IsNullOrEmpty(again))
                    return again;

                return await RequestAndCacheTokenAsync(connection, scope, CancellationToken.None);
            }, cancellationToken);
        }

        public async Task<string> GetTokenAsync(Domain.Entities.Connection connection, string scope, CancellationToken cancellationToken = default)
        {
            var key = cacheService.CreateCacheKey(connection.TenantId.ToString(), connection.ClientId.ToString(), scope);
            var result = await cacheService.GetValueAsync(key);
            return result?.ToString() ?? string.Empty;
        }

        private async Task<string> RequestAndCacheTokenAsync(Domain.Entities.Connection connection, string scope, CancellationToken cancellationToken)
        {
            var credential = CreateClientSecretCredential(
                connection.TenantId,
                connection.ClientId,
                connection.ClientSecret
            );

            var key = cacheService.CreateCacheKey(connection.TenantId.ToString(), connection.ClientId.ToString(), scope);
            var token = await credential.GetTokenAsync(new TokenRequestContext(new[] { scope }), cancellationToken);

            var margin = authenOptions.TokenCacheSafetyMargin;
            var ttl = token.ExpiresOn - DateTimeOffset.UtcNow - margin;
            if (ttl > TimeSpan.Zero)
                await cacheService.SetValueAsync(key, token.Token, ttl);

            return token.Token;
        }
    }
}
