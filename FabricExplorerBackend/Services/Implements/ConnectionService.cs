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
        ITokenService tokenService,
        ISecretProtector secretProtector,
        IUnitOfWork unitOfWork,
        IConnectionMapper connectionMapper) : IConnectionService
    {
        public async Task<ApiResponse<IEnumerable<ConnectionResponse>>> GetAllConnectionsAsync()
        {
            try
            {
                var connectionList = await unitOfWork.ConnectionRepository.GetAllAsync();
                var connectionResponseList = connectionMapper.Map(connectionList);
                return new ApiResponse<IEnumerable<ConnectionResponse>>(StatusCodes.Status200OK,  connectionResponseList);
            }
            catch(Exception Ex)
            {
                return new ApiResponse<IEnumerable<ConnectionResponse>>(StatusCodes.Status500InternalServerError, "Internal Server Error");
            }
        }

        public async Task<ApiResponse<ConnectionResponse>> GetConnectionByIdAsync(Guid connectionId)
        {
            try
            {
                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
                if (connection == null)
                {
                    return new ApiResponse<ConnectionResponse>(StatusCodes.Status404NotFound, "Connection not found");
                }
                var connectionResponse = connectionMapper.Map(connection);
                return new ApiResponse<ConnectionResponse>(StatusCodes.Status200OK, connectionResponse);

            }
            catch (Exception Ex)
            {
                return new ApiResponse<ConnectionResponse>(StatusCodes.Status500InternalServerError, "Internal Server Error");
            }
        }

        public async Task<ApiResponse<ConfirmationResponse>> UpdateConnection(Guid connectionId, UpdateConnectionRequest request)
        {
            try
            {
                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, true);
                if (connection == null)
                {
                    return new ApiResponse<ConfirmationResponse>(StatusCodes.Status404NotFound, "Connection not found");
                }

                if (!string.IsNullOrEmpty(request.ClientSecret))
                    request.ClientSecret = secretProtector.Protect(request.ClientSecret);
                connectionMapper.Map(request, connection);
                connection.UpdatedAt = DateTimeOffset.UtcNow;
                connection.UpdatedBy = null; // You can set this to the current user if you have authentication implemented

                await unitOfWork.ConnectionRepository.Update(connection);
                await unitOfWork.SaveChangesAsync();
                return new ApiResponse<ConfirmationResponse>(StatusCodes.Status200OK, new ConfirmationResponse { Message = "Connection updated successfully" });
            }
            catch (Exception Ex)
            {
                return new ApiResponse<ConfirmationResponse>(StatusCodes.Status500InternalServerError, "Internal Server Error");
            }
        }

        public async Task<ApiResponse<ConfirmationResponse>> TestConnection(Guid connectionId)
        {
            try
            {
                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
                if (connection == null)
                {
                    return new ApiResponse<ConfirmationResponse>(StatusCodes.Status404NotFound, "Connection not found");
                }

                var token = await tokenService.CreateTokenAsync(
                    connection.TenantId, 
                    connection.ClientId, 
                    secretProtector.Unprotect(connection.ClientSecret));

                var context = await contextFactory.CreateFabricContextAsync(connection.TenantId, connectionId, connection.WorkspaceId, token);
                var response = await context.Client.Core.Workspaces.GetWorkspaceAsync(connection.WorkspaceId);

                if (response != null && response.Value.Id == connection.WorkspaceId)
                    return new ApiResponse<ConfirmationResponse>(StatusCodes.Status200OK, new ConfirmationResponse() { Message = "Active connection check succeeded" });
                return new ApiResponse<ConfirmationResponse>(StatusCodes.Status400BadRequest, "Active connection check failed");
            }
            catch (Exception Ex)
            {
                return new ApiResponse<ConfirmationResponse>(StatusCodes.Status500InternalServerError, "Internal Server Error");
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
                await unitOfWork.SaveChangesAsync();

                var connectionResponse = connectionMapper.Map(connection);

                return new ApiResponse<ConnectionResponse>(StatusCodes.Status201Created, connectionResponse);
            }
            catch(Exception Ex)
            {
                return new ApiResponse<ConnectionResponse>(StatusCodes.Status500InternalServerError, "Internal Server Error");
            }
        }

        public async Task<ApiResponse<ConfirmationResponse>> DeleteConnection(Guid connectionId)
        {
            try
            {
                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
                if (connection == null)
                    return new ApiResponse<ConfirmationResponse>(StatusCodes.Status404NotFound, "Connection not found");

                await unitOfWork.ConnectionRepository.Delete(connection);
                await unitOfWork.SaveChangesAsync();

                return new ApiResponse<ConfirmationResponse>(StatusCodes.Status200OK, new ConfirmationResponse { Message = "Connection updated successfully" });
            }
            catch(Exception Ex)
            {
                return new ApiResponse<ConfirmationResponse>(StatusCodes.Status500InternalServerError, "Internal Server Error");
            }
        }

        public async Task<ApiResponse<ConfirmationResponse>> ActiveConnection(Guid targetConnectionId)
        {
            try
            {
                var userId = httpContext?.HttpContext?.User.FindFirstValue("oid");
                if (string.IsNullOrEmpty(userId))
                    return new ApiResponse<ConfirmationResponse>(StatusCodes.Status404NotFound, "User Id not found");

                var user = await unitOfWork.UserRepository.GetByIdAsync(Guid.Parse(userId), true);
                if (user == null)
                    return new ApiResponse<ConfirmationResponse>(StatusCodes.Status404NotFound, "User not found");

                var targetConnection = await unitOfWork.ConnectionRepository.GetByIdAsync(targetConnectionId);

                if (targetConnection == null)
                    return new ApiResponse<ConfirmationResponse>(StatusCodes.Status404NotFound, "Target connection not found");

                user.ActiveConnectionId = targetConnectionId;
                await unitOfWork.SaveChangesAsync();

                return new ApiResponse<ConfirmationResponse>(StatusCodes.Status200OK, new ConfirmationResponse() { Message = "Active connection succeeded" });
            }
            catch (Exception Ex) 
            {
                return new ApiResponse<ConfirmationResponse>(StatusCodes.Status500InternalServerError, "Internal Server Error");
            }
        }
    }
}
