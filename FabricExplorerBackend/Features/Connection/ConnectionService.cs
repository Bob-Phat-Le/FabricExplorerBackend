using Azure;
using Azure.Identity;
using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Requests.Connection;
using FabricExplorerBackend.Commons.Models.Requests.Paginations;
using FabricExplorerBackend.Commons.Models.Responses.Connection;
using FabricExplorerBackend.Commons.Models.Responses.Fabric;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.Paginations;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.Features.Fabric.Context;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using FabricExplorerBackend.Infrastructures.Securities;
using System.Security.Claims;

namespace FabricExplorerBackend.Features.Connection
{
    public class ConnectionService(
        IHttpContextAccessor httpContext,
        IFabricContextFactory contextFactory,
        ISecretProtector secretProtector,
        IUnitOfWork unitOfWork,
        IConnectionMapper connectionMapper) : IConnectionService
    {
        public async Task<Result<PagedResponse<IEnumerable<ConnectionResponse>>>> GetAllConnectionsAsync(PaginationRequest? paginationRequest)
        {
            try
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
                    TotalPages = count % pageSize == 0 ? count / pageSize : count / pageSize + 1
                };

                IEnumerable<ConnectionResponse> connectionResponseList = connectionMapper.Map(connectionList);

                return Result<PagedResponse<IEnumerable<ConnectionResponse>>>
                    .Success(new PagedResponse<IEnumerable<ConnectionResponse>>(connectionResponseList, pageInformation));

            }
            catch (Exception Ex)
            {
                return Result<PagedResponse<IEnumerable<ConnectionResponse>>>
                    .Failure(ResultStatus.InternalError, "Internal Server Error");
            }
        }

        public async Task<Result<ConnectionResponse>> GetConnectionByIdAsync(Guid connectionId)
        {
            try
            {
                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
                if (connection == null)
                {
                    return Result<ConnectionResponse>.Failure(ResultStatus.NotFound, "Connection not found");
                }
                var connectionResponse = connectionMapper.Map(connection);
                return Result<ConnectionResponse>.Success(connectionResponse);
            }
            catch (Exception Ex)
            {
                return Result<ConnectionResponse>.Failure(ResultStatus.InternalError, "Internal Server Error");
            }
        }

        public async Task<Result<ConfirmationResponse>> UpdateConnection(Guid connectionId, UpdateConnectionRequest request)
        {
            try
            {
                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, true);
                if (connection == null)
                {
                    return Result<ConfirmationResponse>.Failure(ResultStatus.NotFound, "Connection not found");
                }

                if (!string.IsNullOrEmpty(request.ClientSecret))
                    request.ClientSecret = secretProtector.Protect(request.ClientSecret);
                connectionMapper.Map(request, connection);
                connection.UpdatedAt = DateTimeOffset.UtcNow;
                connection.UpdatedBy = null; // You can set this to the current user if you have authentication implemented

                await unitOfWork.ConnectionRepository.Update(connection);
                var lineChanges = await unitOfWork.SaveChangesAsync();

                if (lineChanges > 0)
                    return Result<ConfirmationResponse>.Success(new ConfirmationResponse { Message = "Connection updated successfully" });
                return Result<ConfirmationResponse>.Failure(ResultStatus.BadRequest, "Failed to update connection");
            }
            catch (Exception Ex)
            {
                return Result<ConfirmationResponse>.Failure(ResultStatus.InternalError, "Internal Server Error");
            }
        }

        public async Task<Result<ConfirmationResponse>> TestConnection(Guid connectionId)
        {
            try
            {
                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
                if (connection == null)
                {
                    return Result<ConfirmationResponse>.Failure(ResultStatus.NotFound, "Connection not found");
                }

                var context = await contextFactory.CreateFabricContextAsync(connection);
                if (context == null)
                    return Result<ConfirmationResponse>.Failure(ResultStatus.BadRequest, "Can not create context with this connection id");

                var response = await context.Client.Core.Workspaces.GetWorkspaceAsync(context.WorkspaceId);

                if (response != null && response.Value.Id == connection.WorkspaceId)
                    return Result<ConfirmationResponse>.Success(new ConfirmationResponse() { Message = "Connection check succeeded" });
                return Result<ConfirmationResponse>.Failure(ResultStatus.BadRequest, "Failed to check connection");
            }
            catch (AuthenticationFailedException Ex)
            {
                return Result<ConfirmationResponse>.Failure(ResultStatus.BadRequest, Ex.Message);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return Result<ConfirmationResponse>.Failure(
                    ResultStatus.NotFound,
                    "Workspace not found or you do not have access to it.");
            }
            catch (Exception Ex)
            {
                return Result<ConfirmationResponse>.Failure(ResultStatus.InternalError, Ex.Message);
            }
        }

        public async Task<Result<ConfirmationResponse>> CreateConnectionAsync(CreateConnectionRequest request)
        {
            try
            {
                var connection = connectionMapper.Map(request);
                connection.CreatedAt = DateTimeOffset.UtcNow;
                connection.CreatedBy = Guid.Empty;
                connection.ClientSecret = secretProtector.Protect(connection.ClientSecret);

                await unitOfWork.ConnectionRepository.AddAsync(connection);
                var lineChanges = await unitOfWork.SaveChangesAsync();

                if (lineChanges > 0)
                {
                    var connectionResponse = connectionMapper.Map(connection);
                    return Result<ConfirmationResponse>.Success(new ConfirmationResponse { Message = "Connection created successfully" });
                }

                return Result<ConfirmationResponse>.Failure(ResultStatus.BadRequest, "Failed to create connection");
            }
            catch (Exception Ex)
            {
                return Result<ConfirmationResponse>.Failure(ResultStatus.InternalError, "Internal Server Error");
            }
        }

        public async Task<Result<ConfirmationResponse>> DeleteConnection(Guid connectionId)
        {
            try
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
            catch (Exception Ex)
            {
                return Result<ConfirmationResponse>.Failure(ResultStatus.InternalError, "Internal Server Error");
            }
        }

        public async Task<Result<ConfirmationResponse>> ActiveConnection(Guid targetConnectionId)
        {
            try
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
            catch (Exception Ex)
            {
                return Result<ConfirmationResponse>.Failure(ResultStatus.InternalError, "Internal Server Error");
            }
        }

        public async Task<Result<ConnectionResponse>> GetActiveConnectionAsync()
        {
            try
            {
                var userId = httpContext?.HttpContext?.User.FindFirstValue("oid");
                if (string.IsNullOrEmpty(userId))
                    return Result<ConnectionResponse>.Failure(ResultStatus.NotFound, "User id not found");

                var user = await unitOfWork.UserRepository.GetByIdWithConnectionAsync(Guid.Parse(userId));
                if (user == null)
                    return Result<ConnectionResponse>.Failure(ResultStatus.NotFound, "User not found");

                return Result<ConnectionResponse>.Success(connectionMapper.Map(user.ActiveConnection));
            }
            catch (Exception Ex)
            {
                return Result<ConnectionResponse>.Failure(ResultStatus.InternalError, "Internal Server Error");
            }
        }
    }
}
