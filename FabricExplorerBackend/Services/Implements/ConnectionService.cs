using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Enums;
using FabricExplorerBackend.Mappers.Connection;
using FabricExplorerBackend.Models.Requests.Connection;
using FabricExplorerBackend.Models.Responses;
using FabricExplorerBackend.Models.Responses.Connection;
using FabricExplorerBackend.Repositories.Interfaces;
using FabricExplorerBackend.Securities;
using FabricExplorerBackend.Services.Interfaces;
using System.Security.Claims;

namespace FabricExplorerBackend.Services.Implements
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
                var connectionList = await unitOfWork.ConnectionRepository.GetAllAsync();
                var pageInformation = new PageInformation();

                if (paginationRequest != null)
                {
                    pageInformation.Page = paginationRequest.Page;
                    pageInformation.PageSize = paginationRequest.PageSize;

                    if (pageInformation.Page * pageInformation.PageSize < connectionList.Count())
                        pageInformation.HasNextPage = true;
                    else
                        pageInformation.HasNextPage = false;

                    connectionList = connectionList.Skip((pageInformation.Page - 1) * pageInformation.PageSize).Take(pageInformation.PageSize);

                    pageInformation.Total = connectionList.Count();
                }

                IEnumerable<ConnectionResponse> connectionResponseList = connectionMapper.Map(connectionList);

                return Result<PagedResponse<IEnumerable<ConnectionResponse>>>
                    .Success(new PagedResponse<IEnumerable<ConnectionResponse>>(connectionResponseList, pageInformation));

            }
            catch(Exception Ex)
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
            catch(Exception Ex)
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
            catch(Exception Ex)
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
