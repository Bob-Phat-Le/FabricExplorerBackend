using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Models.Requests.Connection;
using FabricExplorerBackend.Models.Responses;
using FabricExplorerBackend.Models.Responses.Connection;

namespace FabricExplorerBackend.Services.Interfaces
{
    public interface IConnectionService
    {
        Task<ApiResponse<ConnectionResponse>> CreateConnectionAsync(CreateConnectionRequest request);
        Task<ApiResponse<PagedResponse<IEnumerable<ConnectionResponse>>>> GetAllConnectionsAsync(paginationRequest? paginationRequest);
        Task<ApiResponse<ConnectionResponse>> GetConnectionByIdAsync(Guid connectionId);
        Task<ApiResponse<ConfirmationResponse>> UpdateConnection(Guid connectionId, UpdateConnectionRequest request);
        Task<ApiResponse<ConfirmationResponse>> DeleteConnection(Guid connectionId);
        Task<ApiResponse<ConfirmationResponse>> TestConnection(Guid connectionId);
        Task<ApiResponse<ConfirmationResponse>> ActiveConnection(Guid targetConnectionId);
    }
}
