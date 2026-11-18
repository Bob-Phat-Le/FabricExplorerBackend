using FabricExplorerBackend.Application.MirroredDatabase.Contracts;
using FabricExplorerBackend.Commons.Contracts;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.Results;

namespace FabricExplorerBackend.Application.MirroredDatabase
{
    public partial class MirroredDatabaseService
    {
        public async Task<Result<LandingZoneListResponse>> ListObjectsInLandingZoneAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, string? path, LandingZoneRequestQuery query, CancellationToken cancellationToken = default)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection == null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

            var client = fabricRestClientFactory.CreateFabricRestClient(connection, Infrastructures.Fabric.FabricRestClient.FabricApiAudience.Storage);

            var baseUri = $"https://onelake.dfs.fabric.microsoft.com/{workspaceId}/{mirroredDatabaseId}/Files/LandingZone";
            var uri = string.IsNullOrEmpty(path) ? $"{baseUri}?resource={query.Resource}&recursive={query.Recursive.ToString().ToLower()}" : $"{baseUri}/{path}?resource={query.Resource}&recursive={query.Recursive.ToString().ToLower()}";

            var result = await client.GetAsync<LandingZoneItemList>(uri.ToString(), cancellationToken: cancellationToken);

            return Result<LandingZoneListResponse>.Success(new LandingZoneListResponse
            {
                Paths = result.Paths.Select(mapper.ToLandingZoneItemResponse).ToList(),
                ContinuationToken = result.ContinuationToken
            });
        }

        public async Task<Result<ConfirmationResponse>> CreateFolderAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, string? path, CancellationToken cancellationToken = default)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection == null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

            if (string.IsNullOrEmpty(path))
                return Error.BadRequest(ErrorCodes.Fabric.LandingZone.BadRequest, "Folder name is required");

            var client = fabricRestClientFactory.CreateFabricRestClient(connection, Infrastructures.Fabric.FabricRestClient.FabricApiAudience.Storage);

            var baseUri = $"https://onelake.dfs.fabric.microsoft.com/{workspaceId}/{mirroredDatabaseId}/Files/LandingZone/{path.TrimEnd("/")}?resource=directory";

            await client.PutAsync(baseUri, cancellationToken: cancellationToken);

            return Result<ConfirmationResponse>.Success(new ConfirmationResponse { Message = "Folder created successfully" });
        }

        public async Task<Result<ConfirmationResponse>> DeleteFolderAsync(Guid connectionId, Guid workspaceId, Guid mirroredDatabaseId, string? path, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();

            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection == null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

            var baseUri = $"https://onelake.dfs.fabric.microsoft.com";

            var credential = tokenService.CreateClientSecretCredential(connection.TenantId, connection.ClientId, connection.ClientSecret);
            var client = azureClientFactory.CreateClient(new Uri(baseUri), credential);
            var filesystemClient = client.GetFileSystemClient(workspaceId.ToString());
            var directoryClient = filesystemClient.GetDirectoryClient($"{mirroredDatabaseId}/Files/LandingZone/{path?.TrimEnd('/')}");

            await directoryClient.DeleteAsync(cancellationToken: cancellationToken);

            return Result<ConfirmationResponse>.Success(new ConfirmationResponse { Message = "Folder deleted successfully" });
        }
    }
}
