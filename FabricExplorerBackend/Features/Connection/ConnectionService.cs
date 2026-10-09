using Azure;
using Azure.Identity;
using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Requests.Connection;
using FabricExplorerBackend.Commons.Models.Requests.Paginations;
using FabricExplorerBackend.Commons.Models.Responses.Connection;
using FabricExplorerBackend.Commons.Models.Responses.Fabric;
using FabricExplorerBackend.Commons.Models.Responses.Paginations;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.Features.Cache;
using FabricExplorerBackend.Features.Fabric.Context;
using FabricExplorerBackend.Features.Token;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using FabricExplorerBackend.Infrastructures.Securities;
using System.Security.Claims;

namespace FabricExplorerBackend.Features.Connection
{
    public class ConnectionService(
        IConfiguration configuration,
        ITokenService tokenService,
        ICacheService cacheService,
        IHttpContextAccessor httpContext,
        IFabricContextFactory contextFactory,
        ISecretProtector secretProtector,
        IUnitOfWork unitOfWork,
        IConnectionMapper connectionMapper) : IConnectionService
    {
        public async Task<Result<PagedResponse<IEnumerable<ConnectionResponse>>>> GetAllConnectionsAsync(PaginationRequest? paginationRequest)
        {
            if (paginationRequest != null)
                paginationRequest.Validate();

            var page = paginationRequest?.Page ?? 1;
            var pageSize = paginationRequest!.PageSize;

            var (connectionList, count) = await unitOfWork.ConnectionRepository.GetAllAsync((page - 1) * pageSize, pageSize);

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

        public async Task<Result<ConnectionResponse>> GetConnectionByIdAsync(Guid connectionId)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
            if (connection == null)
                return Result<ConnectionResponse>.Failure(ResultStatus.NotFound, "Connection not found");
            var connectionResponse = connectionMapper.Map(connection);
            return Result<ConnectionResponse>.Success(connectionResponse);
        }

        public async Task<Result<ConfirmationResponse>> UpdateConnection(Guid connectionId, UpdateConnectionRequest request)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, true);
            if (connection == null)
                return Result<ConfirmationResponse>.Failure(ResultStatus.NotFound, "Connection not found");

            if (request.WorkspaceId.HasValue && request.WorkspaceId != connection.WorkspaceId)
            {
                var conn = await unitOfWork.ConnectionRepository.CheckDuplicateWorkspaceIdAsync((Guid)request.WorkspaceId!);
                if (conn != null && conn.Id != connection.Id)
                    return Result<ConfirmationResponse>.Failure(ResultStatus.Conflict, "Workspace id is duplicated");
            }

            if (!string.IsNullOrEmpty(request.ClientSecret))
                request.ClientSecret = secretProtector.Protect(request.ClientSecret);
            connectionMapper.Map(request, connection);
            connection.UpdatedAt = DateTimeOffset.UtcNow;
            connection.UpdatedBy = null; // You can set this to the current user if you have authentication implemented

            await unitOfWork.ConnectionRepository.Update(connection);
            var lineChanges = await unitOfWork.SaveChangesAsync();

            if (lineChanges > 0)
            {
                var cacheKey = cacheService.CreateCacheKey(connectionId.ToString());
                await cacheService.SetValueAsync(cacheKey, System.Text.Json.JsonSerializer.Serialize(connection));
                return Result<ConfirmationResponse>.Success(new ConfirmationResponse { Message = "Connection updated successfully" });
            }
            return Result<ConfirmationResponse>.Failure(ResultStatus.BadRequest, "Failed to update connection");
        }

        public async Task<Result<ConfirmationResponse>> TestConnection(Guid connectionId)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
            if (connection == null)
                return Result<ConfirmationResponse>.Failure(ResultStatus.NotFound, "Connection not found");

            string accessToken;
            try
            {
                accessToken = await tokenService.CreateTokenAsync(
                    connection,
                    configuration.GetValue<string>("Scopes:api.fabric", "https://api.fabric.microsoft.com/.default")!);
            }
            catch (Exception ex)
            {
                return Result<ConfirmationResponse>.Failure(ResultStatus.BadRequest,
                [
                    "Failed to get an access token (check tenant id, client id, client secret).",
                    $"{ex.GetType().Name}: {ex.Message}"
                ]);
            }

            try
            {
                var context = await contextFactory.CreateFabricContextWithTokenAsync(connection, accessToken);
                if (context == null)
                    return Result<ConfirmationResponse>.Failure(ResultStatus.BadRequest, "Can not create context with this connection id");

                var workspace = (await context.Client.Core.Workspaces.GetWorkspaceAsync(context.WorkspaceId)).Value;

                if (workspace.Id != connection.WorkspaceId)
                    return Result<ConfirmationResponse>.Failure(ResultStatus.BadRequest,
                        $"Fabric returned workspace {workspace.Id} but {connection.WorkspaceId} is configured");

                return Result<ConfirmationResponse>.Success(new ConfirmationResponse
                {
                    Message = $"Connection check succeeded. Workspace: {workspace.DisplayName} ({workspace.Id})"
                });
            }
            catch (RequestFailedException ex)
            {
                var status = ex.Status switch
                {
                    401 or 403 => ResultStatus.Forbidden,
                    404 => ResultStatus.NotFound,
                    _ => ResultStatus.BadRequest
                };

                var hint = ex.Status switch
                {
                    403 => "The service principal is not allowed. Add it to the workspace and enable \"Service principals can use Fabric APIs\".",
                    404 => "Workspace does not exist, or the service principal is not a member of it.",
                    _ => null
                };

                var errors = new List<string>
                {
                    "Calling Fabric failed.",
                    $"HTTP {ex.Status}, error code: {ex.ErrorCode ?? "n/a"}",
                    ex.Message
                };
                if (hint != null) errors.Add($"Hint: {hint}");

                return Result<ConfirmationResponse>.Failure(status, errors);
            }
            catch (Exception ex)
            {
                return Result<ConfirmationResponse>.Failure(ResultStatus.InternalError,
                [
                    "Calling Fabric failed.",
                    $"{ex.GetType().Name}: {ex.Message}"
                ]);
            }
        }

        public async Task<Result<ConfirmationResponse>> CreateConnectionAsync(CreateConnectionRequest request)
        {
            var connection = connectionMapper.Map(request);
            connection.CreatedAt = DateTimeOffset.UtcNow;
            connection.CreatedBy = Guid.Empty;
            connection.ClientSecret = secretProtector.Protect(connection.ClientSecret);

            var conn = await unitOfWork.ConnectionRepository.CheckDuplicateWorkspaceIdAsync(request.WorkspaceId);
            if (conn != null)
                return Result<ConfirmationResponse>.Failure(ResultStatus.Conflict, "Workspace id is duplicated");

            await unitOfWork.ConnectionRepository.AddAsync(connection);
            var lineChanges = await unitOfWork.SaveChangesAsync();

            if (lineChanges > 0)
            {
                var connectionResponse = connectionMapper.Map(connection);
                return Result<ConfirmationResponse>.Success(new ConfirmationResponse { Message = "Connection created successfully" });
            }

            return Result<ConfirmationResponse>.Failure(ResultStatus.BadRequest, "Failed to create connection");
        }

        public async Task<Result<ConfirmationResponse>> DeleteConnection(Guid connectionId)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
            if (connection == null)
                return Result<ConfirmationResponse>.Failure(ResultStatus.NotFound, "Connection not found");

            await unitOfWork.ConnectionRepository.Delete(connection);
            var lineChanges = await unitOfWork.SaveChangesAsync();

            if (lineChanges > 0)
                return Result<ConfirmationResponse>.Success(new ConfirmationResponse { Message = "Connection deleted successfully" });

            return Result<ConfirmationResponse>.Failure(ResultStatus.BadRequest, "Failed to delete connection");
        }

        public async Task<Result<ConfirmationResponse>> ActiveConnection(Guid targetConnectionId)
        {
            var userId = httpContext?.HttpContext?.User.FindFirstValue("oid");
            if (string.IsNullOrEmpty(userId))
                return Result<ConfirmationResponse>.Failure(ResultStatus.NotFound, "User id not found");

            var user = await unitOfWork.UserRepository.GetByIdAsync(Guid.Parse(userId), true);
            if (user == null)
                return Result<ConfirmationResponse>.Failure(ResultStatus.NotFound, "User not found");

            var targetConnection = await unitOfWork.ConnectionRepository.GetByIdAsync(targetConnectionId);

            if (targetConnection == null)
                return Result<ConfirmationResponse>.Failure(ResultStatus.NotFound, "Target connection not found");

            user.ActiveConnectionId = targetConnectionId;
            var lineChanges = await unitOfWork.SaveChangesAsync();

            if (lineChanges > 0)
                return Result<ConfirmationResponse>.Success(new ConfirmationResponse() { Message = "Active connection succeeded" });

            return Result<ConfirmationResponse>.Failure(ResultStatus.BadRequest, "Failed active connection");
        }

        public async Task<Result<ConnectionResponse>> GetActiveConnectionAsync()
        {
            var userId = httpContext?.HttpContext?.User.FindFirstValue("oid");
            if (string.IsNullOrEmpty(userId))
                return Result<ConnectionResponse>.Failure(ResultStatus.NotFound, "User id not found");

            var user = await unitOfWork.UserRepository.GetByIdWithConnectionAsync(Guid.Parse(userId));
            if (user == null)
                return Result<ConnectionResponse>.Failure(ResultStatus.NotFound, "User not found");

            return Result<ConnectionResponse>.Success(connectionMapper.Map(user.ActiveConnection));
        }
    }
}
