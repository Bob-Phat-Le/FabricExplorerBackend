using Azure.Core;
using Azure.Identity;
using FabricExplorerBackend.Features.Cache;
using FabricExplorerBackend.Infrastructures.Securities;

namespace FabricExplorerBackend.Features.Token
{
    public class TokenService(
        ICacheService cacheService,
        ISecretProtector secretProtector,
        ISingleFlight singleFlight,
        IConfiguration configuration) : ITokenService
    {
        // Luôn xin token mới và ghi đè cache
        public Task<string> CreateTokenAsync(Domain.Entities.Connection connection, string scope, CancellationToken cancellationToken = default)
            => RequestAndCacheTokenAsync(connection, scope, cancellationToken);

        public async Task<string> GetOrCreateTokenAsync(Domain.Entities.Connection connection, string scope, CancellationToken cancellationToken = default)
        {
            var key = CreateKey(connection, scope);
            var cached = (await cacheService.GetValueAsync(key))?.ToString();
            if (!string.IsNullOrEmpty(cached))
                return cached;

            // Khi cache trống, nhiều request/bảng chạy song song sẽ cùng thấy trống (vd. lần đầu mở dropdown).
            // Gộp lại để chỉ MỘT lần xin token tới Entra, những lần còn lại dùng chung kết quả.
            return await singleFlight.RunAsync($"token:{key}", async () =>
            {
                var again = (await cacheService.GetValueAsync(key))?.ToString();
                if (!string.IsNullOrEmpty(again))
                    return again;

                // Dùng chung giữa nhiều request nên không gắn với token của request nào
                return await RequestAndCacheTokenAsync(connection, scope, CancellationToken.None);
            }, cancellationToken);
        }

        public async Task<string> GetTokenAsync(Domain.Entities.Connection connection, string scope, CancellationToken cancellationToken = default)
        {
            var result = await cacheService.GetValueAsync(CreateKey(connection, scope));
            return result?.ToString() ?? string.Empty;
        }

        // PRIVATE AREA

        private async Task<string> RequestAndCacheTokenAsync(Domain.Entities.Connection connection, string scope, CancellationToken cancellationToken)
        {
            var credential = new ClientSecretCredential(
                connection.TenantId.ToString(),
                connection.ClientId.ToString(),
                secretProtector.Unprotect(connection.ClientSecret)
            );

            var token = await credential.GetTokenAsync(new TokenRequestContext(new[] { scope }), cancellationToken);

            // Cache ngắn hơn hạn thật của token một khoảng đệm: tránh trả token chỉ còn vài giây (hết hạn giữa chừng một request)
            var margin = TimeSpan.FromMinutes(configuration.GetValue("Fabric:TokenCacheSafetyMarginMinutes", 5));
            var ttl = token.ExpiresOn - DateTimeOffset.UtcNow - margin;
            if (ttl > TimeSpan.Zero)
                await cacheService.SetValueAsync(CreateKey(connection, scope), token.Token, ttl);

            return token.Token;
        }

        // Token được cấp theo tenant + client + scope, không phụ thuộc workspace
        private string CreateKey(Domain.Entities.Connection connection, string scope)
            => cacheService.CreateCacheKey(
                "token",
                connection.TenantId.ToString(),
                connection.ClientId.ToString(),
                scope);
    }
}
