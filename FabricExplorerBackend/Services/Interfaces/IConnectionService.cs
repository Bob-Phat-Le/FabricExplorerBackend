using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Models.Requests.Connection;
using FabricExplorerBackend.Models.Responses;
using FabricExplorerBackend.Models.Responses.Connection;

namespace FabricExplorerBackend.Services.Interfaces
{
    public interface IConnectionService
    {
        Task<Result<ConfirmationResponse>> CreateConnectionAsync(CreateConnectionRequest request);
        Task<Result<PagedResponse<IEnumerable<ConnectionResponse>>>> GetAllConnectionsAsync(PaginationRequest? paginationRequest);
        Task<Result<ConnectionResponse>> GetConnectionByIdAsync(Guid connectionId);
        Task<Result<ConnectionResponse>> GetActiveConnectionAsync();
        Task<Result<ConfirmationResponse>> UpdateConnection(Guid connectionId, UpdateConnectionRequest request);
        Task<Result<ConfirmationResponse>> DeleteConnection(Guid connectionId);
        Task<Result<ConfirmationResponse>> TestConnection(Guid connectionId);
        Task<Result<ConfirmationResponse>> ActiveConnection(Guid targetConnectionId);
    }
}
