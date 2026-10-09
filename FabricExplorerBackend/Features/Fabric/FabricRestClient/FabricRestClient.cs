using FabricExplorerBackend.Commons.Models.Responses.Fabric.Exceptions;
using FabricExplorerBackend.Features.Token;

namespace FabricExplorerBackend.Features.Fabric.FabricRestClient
{
    public class FabricRestClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ITokenService tokenService,
        Domain.Entities.Connection connection) : IFabricRestClient
    {
        private string scope = configuration.GetValue<string>("Scopes:api.fabric")!;

        public async Task<TResponse?> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
        {
            var token = await tokenService.GetOrCreateTokenAsync(connection, scope, cancellationToken);

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            using var response = await httpClient.GetAsync(endpoint, cancellationToken);

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException)
            {
                //var body = await response.Content.ReadFromJsonAsync<FabricErrorResponse>();
                //throw JsonSerializer.Deserialize<FabricErrorResponse>(body!)!;
                throw await response.Content.ReadFromJsonAsync<FabricErrorResponse>();
            }

            return await response.Content
                .ReadFromJsonAsync<TResponse>(cancellationToken);
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
        {
            var token = await tokenService.GetOrCreateTokenAsync(connection, scope, cancellationToken);

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            using var response = await httpClient.PostAsJsonAsync(endpoint, request, cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<TResponse>(cancellationToken);
        }

        public async Task<TResponse?> PatchAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
        {
            var token = await tokenService.GetOrCreateTokenAsync(connection, scope, cancellationToken);

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            using var response = await httpClient.PatchAsJsonAsync(endpoint, request, cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<TResponse>(cancellationToken);
        }

        public async Task DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
        {
            var token = await tokenService.GetOrCreateTokenAsync(connection, scope, cancellationToken);

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            using var response = await httpClient.DeleteAsync(endpoint, cancellationToken);

            response.EnsureSuccessStatusCode();
        }
    }
}
