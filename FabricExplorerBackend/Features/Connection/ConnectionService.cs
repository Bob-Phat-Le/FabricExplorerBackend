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
using FabricExplorerBackend.Helpers;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using FabricExplorerBackend.Infrastructures.Securities;
using System.Security.Claims;

namespace FabricExplorerBackend.Features.Connection
{
    public class ConnectionService(
        ICacheService cacheService,
        ICredentialHelper credentialHelper,
        IHttpContextAccessor httpContext,
        IFabricContextFactory contextFactory,
        ISecretProtector secretProtector,
        IUnitOfWork unitOfWork,
        IConnectionMapper connectionMapper) : IConnectionService
    {
        public async Task<Result<PagedResponse<IEnumerable<ConnectionResponse>>>> GetAllConnectionsAsync(PaginationRequest? paginationRequest)
        {
            if (paginationRequest != null && !paginationRequest.IsValid())
                return Result<PagedResponse<IEnumerable<ConnectionResponse>>>.Failure(ResultStatus.BadRequest, "Invalid pagination request");

            var page = (int)paginationRequest?.Page!;
            var pageSize = (int)paginationRequest?.PageSize!;

            var (connectionList, count) = await unitOfWork.ConnectionRepository.GetAllAsync((page - 1) * pageSize, pageSize);

            var pageInformation = new PageInformation
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = connectionList.Count(),
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

            if (!string.IsNullOrEmpty(request.ClientSecret))
                request.ClientSecret = secretProtector.Protect(request.ClientSecret);
            connectionMapper.Map(request, connection);
            connection.UpdatedAt = DateTimeOffset.UtcNow;
            connection.UpdatedBy = null; // You can set this to the current user if you have authentication implemented

            if (request.WorkspaceId != Guid.Empty)
            {
                var conn = await unitOfWork.ConnectionRepository.CheckDuplicateWorkspaceIdAsync((Guid)request.WorkspaceId!);
                if (conn != null && conn.Id != connection.Id)
                    return Result<ConfirmationResponse>.Failure(ResultStatus.ValidationError, "Workspace id is duplicated");
            }

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
            try
            {
                var connection = await credentialHelper.GetCredentialWithConnectionId(connectionId);
                if (connection == null)
                    return Result<ConfirmationResponse>.Failure(ResultStatus.NotFound, "Connection not found");

                var context = await contextFactory.CreateFabricContextAsync(connection);
                if (context == null)
                    return Result<ConfirmationResponse>.Failure(ResultStatus.BadRequest, "Can not create context with this connection id");

                var response = await context.Client.Core.Workspaces.GetWorkspaceAsync(context.WorkspaceId);

                if (response == null)
                    return Result<ConfirmationResponse>.Failure(ResultStatus.NotFound, "Workspace not found");

                if (response != null && response.Value.Id == connection.WorkspaceId)
                    return Result<ConfirmationResponse>.Success(new ConfirmationResponse() { Message = "Connection check succeeded" });
                return Result<ConfirmationResponse>.Failure(ResultStatus.BadRequest, "Failed to check connection");
            }
            catch (AuthenticationFailedException ex)
            {
                return Result<ConfirmationResponse>.Failure(ResultStatus.BadRequest, ex.Message);
            }
            catch (RequestFailedException ex) when (ex.Status is 401 or 403)
            {
                return Result<ConfirmationResponse>.Failure(ResultStatus.Forbidden, "The connection is not allowed to list workspaces.");
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
                return Result<ConfirmationResponse>.Failure(ResultStatus.ValidationError, "Workspace id is duplicated");

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
