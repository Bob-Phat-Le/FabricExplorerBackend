using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Requests.Connection;
using FabricExplorerBackend.Commons.Models.Responses;
using FabricExplorerBackend.Commons.Models.Responses.Connection;

namespace FabricExplorerBackend.Features.Connection
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
