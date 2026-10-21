using FabricExplorerBackend.Application.Connection.Contracts;
using FabricExplorerBackend.Commons.Contracts;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.Paginations;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Infrastructures.Cache;
using FabricExplorerBackend.Infrastructures.Fabric.Client;
using FabricExplorerBackend.Infrastructures.Fabric.Context;
using FabricExplorerBackend.Infrastructures.Identity;
using FabricExplorerBackend.Infrastructures.Options;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using FabricExplorerBackend.Infrastructures.Security;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace FabricExplorerBackend.Application.Connection
{
    public class ConnectionService(
        IOptions<AzureOptions> azureOptions,
        ITokenService tokenService,
        ICacheService cacheService,
        IHttpContextAccessor httpContext,
        IClientFactory clientFactory,
        IFabricContextFactory contextFactory,
        ISecretProtector secretProtector,
        IUnitOfWork unitOfWork,
        IConnectionMapper connectionMapper) : IConnectionService
    {
        private AzureOptions azureOptions = azureOptions.Value;

        public async Task<Result<PagedResponse<IEnumerable<ConnectionResponse>>>> GetAllConnectionsAsync(PaginationRequest? paginationRequest, CancellationToken cancellationToken = default)
        {
            if (paginationRequest != null)
                paginationRequest.Validate();

            var page = paginationRequest?.Page ?? 1;
            var pageSize = paginationRequest!.PageSize;

            var (connectionList, count) = await unitOfWork.ConnectionRepository.GetAllAsync((page - 1) * pageSize, pageSize, cancellationToken: cancellationToken);

            var pageInformation = new PageInformation
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = count,
                ItemsCount = connectionList.Count(),
                TotalPages = (int)Math.Ceiling((decimal)count / pageSize)
            };

            IEnumerable<ConnectionResponse> connectionResponseList = connectionMapper.Map(connectionList);

            return Result<PagedResponse<IEnumerable<ConnectionResponse>>>
                .Success(new PagedResponse<IEnumerable<ConnectionResponse>>(connectionResponseList, pageInformation));
        }

        public async Task<Result<ConnectionResponse>> GetConnectionByIdAsync(Guid connectionId, CancellationToken cancellationToken = default)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection == null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");
            var connectionResponse = connectionMapper.Map(connection);
            return Result<ConnectionResponse>.Success(connectionResponse);
        }

        public async Task<Result<ConfirmationResponse>> UpdateConnection(Guid connectionId, UpdateConnectionRequest request, CancellationToken cancellationToken = default)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, true, cancellationToken);
            if (connection == null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

            if (request.WorkspaceId.HasValue && request.WorkspaceId != connection.WorkspaceId)
            {
                var conn = await unitOfWork.ConnectionRepository.CheckDuplicateWorkspaceIdAsync((Guid)request.WorkspaceId!, cancellationToken: cancellationToken);
                if (conn != null && conn.Id != connection.Id)
                    return Error.Conflict(ErrorCodes.Connection.Duplicated, "Workspace id is duplicated");
            }

            if (!string.IsNullOrEmpty(request.ClientSecret))
                request.ClientSecret = secretProtector.Protect(request.ClientSecret);
            connectionMapper.Map(request, connection);
            connection.UpdatedAt = DateTimeOffset.UtcNow;
            connection.UpdatedBy = null; // You can set this to the current user if you have authentication implemented

            unitOfWork.ConnectionRepository.Update(connection);
            var lineChanges = await unitOfWork.SaveChangesAsync(cancellationToken);

            if (lineChanges > 0)
            {
                var cacheKey = cacheService.CreateCacheKey(connectionId.ToString());
                await cacheService.SetValueAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(connection));
                return Result<ConfirmationResponse>.Success(new ConfirmationResponse { Message = "Connection updated successfully" });
            }
            throw new InvalidOperationException
                ($"Failed to update connection-{connectionId}");
        }

        public async Task<Result<ConfirmationResponse>> TestConnection(Guid connectionId, CancellationToken cancellationToken = default)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection == null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

            string accessToken;
            accessToken = await tokenService.CreateTokenAsync(
                connection,
                azureOptions.Scopes.Fabric,
                cancellationToken);

            var context = await contextFactory.CreateFabricContextWithTokenAsync(connection, accessToken);

            var workspace = (await context.Client.Core.Workspaces.GetWorkspaceAsync(context.WorkspaceId, cancellationToken: cancellationToken)).Value;

            if (workspace == null || workspace.Id != connection.WorkspaceId)
                return Error.NotFound(ErrorCodes.Fabric.NotFound, "Workspace not found");

            return Result<ConfirmationResponse>.Success(new ConfirmationResponse
            {
                Message = $"Connection check succeeded. Workspace: {workspace.DisplayName} ({workspace.Id})"
            });
        }

        public async Task<Result<ConfirmationResponse>> CreateConnectionAsync(CreateConnectionRequest request, CancellationToken cancellationToken = default)
        {
            var connection = connectionMapper.Map(request);
            connection.CreatedAt = DateTimeOffset.UtcNow;
            connection.CreatedBy = Guid.Empty;
            connection.ClientSecret = secretProtector.Protect(connection.ClientSecret);

            var conn = await unitOfWork.ConnectionRepository.CheckDuplicateWorkspaceIdAsync(request.WorkspaceId, cancellationToken);
            if (conn != null)
                return Error.Conflict(ErrorCodes.Connection.Duplicated, "Workspace id is duplicated");

            await unitOfWork.ConnectionRepository.AddAsync(connection, cancellationToken);
            var lineChanges = await unitOfWork.SaveChangesAsync(cancellationToken);

            if (lineChanges > 0)
            {
                var connectionResponse = connectionMapper.Map(connection);
                return Result<ConfirmationResponse>.Success(new ConfirmationResponse { Message = "Connection created successfully" });
            }

            throw new InvalidOperationException($"Failed to create connection");
        }

        public async Task<Result<ConfirmationResponse>> DeleteConnection(Guid connectionId, CancellationToken cancellationToken = default)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection == null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

            unitOfWork.ConnectionRepository.Delete(connection);
            var lineChanges = await unitOfWork.SaveChangesAsync(cancellationToken);

            if (lineChanges > 0)
                return Result<ConfirmationResponse>.Success(new ConfirmationResponse { Message = "Connection deleted successfully" });

            throw new InvalidOperationException($"Failed to delete connection-{connectionId}");
        }

        public async Task<Result<ConfirmationResponse>> ActiveConnection(Guid targetConnectionId, CancellationToken cancellationToken = default)
        {
            var userId = httpContext?.HttpContext?.User.FindFirstValue("oid");

            var user = await unitOfWork.UserRepository.GetByIdAsync(Guid.Parse(userId!), true);
            if (user == null)
                return Error.NotFound(ErrorCodes.User.NotFound, "User not found");

            var targetConnection = await unitOfWork.ConnectionRepository.GetByIdAsync(targetConnectionId, cancellationToken: cancellationToken);

            if (targetConnection == null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Target connection not found");

            user.ActiveConnectionId = targetConnectionId;
            var lineChanges = await unitOfWork.SaveChangesAsync();

            if (lineChanges > 0)
                return Result<ConfirmationResponse>.Success(new ConfirmationResponse() { Message = "Active connection succeeded" });

            throw new InvalidOperationException($"Failed to active connection-{targetConnectionId}");
        }

        public async Task<Result<ConnectionResponse>> GetActiveConnectionAsync(CancellationToken cancellationToken = default)
        {
            var userId = httpContext?.HttpContext?.User.FindFirstValue("oid");

            var user = await unitOfWork.UserRepository.GetByIdWithConnectionAsync(Guid.Parse(userId!), cancellationToken: cancellationToken);

            if (user == null)
                return Error.NotFound(ErrorCodes.User.NotFound, "User not found");

            return Result<ConnectionResponse>.Success(connectionMapper.Map(user.ActiveConnection));
        }

        public async Task<Result<ConfirmationResponse>> TestConnection(TestConnectionRequest request, CancellationToken cancellationToken = default)
        {
            var credential = tokenService.CreateClientSecretCredential(request.TenantId, request.ClientId, request.ClientSecret, false);
            var client = clientFactory.CreateClientAsync(credential);

            var workspace = (await client.Core.Workspaces.GetWorkspaceAsync(request.WorkspaceId, cancellationToken: cancellationToken)).Value;

            if (workspace == null || workspace.Id != request.WorkspaceId)
                return Error.NotFound(ErrorCodes.Fabric.NotFound, "Workspace not found");

            return Result<ConfirmationResponse>.Success(new ConfirmationResponse
            {
                Message = $"Connection check succeeded. Workspace: {workspace.DisplayName} ({workspace.Id})"
            });
        }
    }
}
