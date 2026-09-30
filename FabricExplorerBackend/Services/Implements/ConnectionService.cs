using FabricExplorerBackend.Commons;
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
        public async Task<ApiResponse<PagedResponse<IEnumerable<ConnectionResponse>>>> GetAllConnectionsAsync(PaginationRequest? paginationRequest)
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

                return new(StatusCodes.Status200OK, new PagedResponse<IEnumerable<ConnectionResponse>>(connectionResponseList, pageInformation));
            }
            catch(Exception Ex)
            {
                return new(StatusCodes.Status500InternalServerError, "Internal Server Error");
            }
        }

        public async Task<ApiResponse<ConnectionResponse>> GetConnectionByIdAsync(Guid connectionId)
        {
            try
            {
                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
                if (connection == null)
                {
                    return new(StatusCodes.Status404NotFound, "Connection not found");
                }
                var connectionResponse = connectionMapper.Map(connection);
                return new(StatusCodes.Status200OK, connectionResponse);

            }
            catch (Exception Ex)
            {
                return new(StatusCodes.Status500InternalServerError, "Internal Server Error");
            }
        }

        public async Task<ApiResponse<ConfirmationResponse>> UpdateConnection(Guid connectionId, UpdateConnectionRequest request)
        {
            try
            {
                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, true);
                if (connection == null)
                {
                    return new(StatusCodes.Status404NotFound, "Connection not found");
                }

                if (!string.IsNullOrEmpty(request.ClientSecret))
                    request.ClientSecret = secretProtector.Protect(request.ClientSecret);
                connectionMapper.Map(request, connection);
                connection.UpdatedAt = DateTimeOffset.UtcNow;
                connection.UpdatedBy = null; // You can set this to the current user if you have authentication implemented

                await unitOfWork.ConnectionRepository.Update(connection);
                var lineChanges = await unitOfWork.SaveChangesAsync();

                if (lineChanges > 0)
                    return new(StatusCodes.Status200OK, new ConfirmationResponse { Message = "Connection updated successfully" });
                return new(StatusCodes.Status400BadRequest, "Failed to update connection");
            }
            catch (Exception Ex)
            {
                return new(StatusCodes.Status500InternalServerError, "Internal Server Error");
            }
        }

        public async Task<ApiResponse<ConfirmationResponse>> TestConnection(Guid connectionId)
        {
            try
            {
                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
                if (connection == null)
                {
                    return new(StatusCodes.Status404NotFound, "Connection not found");
                }

                var context = await contextFactory.CreateFabricContextAsync(connection);
                if (context == null)
                    return new(StatusCodes.Status400BadRequest, "Can not creat context with this connection id");

                var response = await context.Client.Core.Workspaces.GetWorkspaceAsync(context.WorkspaceId);

                if (response != null && response.Value.Id == connection.WorkspaceId)
                    return new(StatusCodes.Status200OK, new ConfirmationResponse() { Message = "Connection check succeeded" });
                return new(StatusCodes.Status400BadRequest, "Failed to check connection");
            }
            catch (Exception Ex)
            {
                return new(StatusCodes.Status500InternalServerError, "Internal Server Error");
            }
        }

        public async Task<ApiResponse<ConnectionResponse>> CreateConnectionAsync(CreateConnectionRequest request)
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
                    return new(StatusCodes.Status201Created, connectionResponse);
                }

                return new(StatusCodes.Status400BadRequest, "Failed to create connection");
            }
            catch(Exception Ex)
            {
                return new(StatusCodes.Status500InternalServerError, "Internal Server Error");
            }
        }

        public async Task<ApiResponse<ConfirmationResponse>> DeleteConnection(Guid connectionId)
        {
            try
            {
                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
                if (connection == null)
                    return new(StatusCodes.Status404NotFound, "Connection not found");

                await unitOfWork.ConnectionRepository.Delete(connection);
                var lineChanges = await unitOfWork.SaveChangesAsync();

                if (lineChanges > 0)
                    return new(StatusCodes.Status200OK, new ConfirmationResponse { Message = "Connection deleted successfully" });

                return new(StatusCodes.Status400BadRequest, "Failed to delete connection");
            }
            catch(Exception Ex)
            {
                return new(StatusCodes.Status500InternalServerError, "Internal Server Error");
            }
        }

        public async Task<ApiResponse<ConfirmationResponse>> ActiveConnection(Guid targetConnectionId)
        {
            try
            {
                var userId = httpContext?.HttpContext?.User.FindFirstValue("oid");
                if (string.IsNullOrEmpty(userId))
                    return new(StatusCodes.Status404NotFound, "User id not found");

                var user = await unitOfWork.UserRepository.GetByIdAsync(Guid.Parse(userId), true);
                if (user == null)
                    return new(StatusCodes.Status404NotFound, "User not found");

                var targetConnection = await unitOfWork.ConnectionRepository.GetByIdAsync(targetConnectionId);

                if (targetConnection == null)
                    return new(StatusCodes.Status404NotFound, "Target connection not found");

                user.ActiveConnectionId = targetConnectionId;
                var lineChanges = await unitOfWork.SaveChangesAsync();
                
                if (lineChanges > 0)
                    return new(StatusCodes.Status200OK, new ConfirmationResponse() { Message = "Active connection succeeded" });
                   
                return new(StatusCodes.Status200OK, new ConfirmationResponse() { Message = "Failed active connection" });
            }
            catch (Exception Ex) 
            {
                return new(StatusCodes.Status500InternalServerError, "Internal Server Error");
            }
        }
    }
}
