using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Infrastructures.Identity;
using FabricExplorerBackend.Infrastructures.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FabricExplorerBackend.Infrastructures.Fabric.FabricRestClient
{
    public class FabricRestClient(
        HttpClient httpClient,
        IOptions<AzureOptions> azureOptions,
        ITokenService tokenService,
        Domain.Entities.Connection connection) : IFabricRestClient
    {
        private const int MaxBodyChars = 4000;
        private AzureOptions azureOptions = azureOptions.Value;

        public async Task<TResponse?> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
        {
            using var response = await SendAsync(HttpMethod.Get, endpoint, null, cancellationToken);
            return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken);
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
        {
            using var response = await SendAsync(HttpMethod.Post, endpoint, null, cancellationToken);
            return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken);
        }

        public async Task<TResponse?> PatchAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
        {
            using var response = await SendAsync(HttpMethod.Patch, endpoint, null, cancellationToken);
            return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken);
        }

        public async Task DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
        {
            using var response = await SendAsync(HttpMethod.Delete, endpoint, null, cancellationToken);
            //return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken);
        }


        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string endpoint, object? body, CancellationToken ct)
        {
            var operation = $"{method} {endpoint}";
            var token = await tokenService.GetOrCreateTokenAsync(connection, azureOptions.Scopes.Fabric, ct);

            using var request = new HttpRequestMessage(method, endpoint)
            {
                Headers = { Authorization = new("Bearer", token) },
                Content = body is null ? null : JsonContent.Create(body)
            };

            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(request, ct);
            }
            catch (HttpRequestException ex)
            {
                throw new ExternalServiceException("FabricApi", operation, null, null, null, ex.Message, null, ex);
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                throw new ExternalServiceException("FabricApi", operation, 504, "Timeout", null, "Request timed out", null, ex);
            }

            if (response.IsSuccessStatusCode) return response;

            var error = await ToExceptionAsync(operation, response, ct);
            response.Dispose();
            throw error;
        }

        private static async Task<ExternalServiceException> ToExceptionAsync(string operation, HttpResponseMessage response, CancellationToken ct)
        {
            var raw = await response.Content.ReadAsStringAsync(ct);
            string? code = null, message = null, requestId = null;
            try
            {
                using var doc = JsonDocument.Parse(raw);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    var root = doc.RootElement;
                    code = root.TryGetProperty("errorCode", out var c) ? c.GetString() : null;
                    message = root.TryGetProperty("message", out var m) ? m.GetString() : null;
                    requestId = root.TryGetProperty("requestId", out var r) ? r.GetString() : null;
                }
            }
            catch (JsonException) { }

            requestId ??= response.Headers.TryGetValues("RequestId", out var v) ? v.FirstOrDefault() : null;
            var body = raw.Length > MaxBodyChars ? raw[..MaxBodyChars] + "...(truncated)" : raw;
            return new ExternalServiceException("FabricApi", operation, (int)response.StatusCode, code, requestId, message, body);
        }
    }
}
