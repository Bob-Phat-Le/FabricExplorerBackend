using FabricExplorerBackend.Application.Connection.Contracts;
using FabricExplorerBackend.Commons.Contracts;
using FabricExplorerBackend.Commons.Paginations;
using FabricExplorerBackend.Commons.Results;

namespace FabricExplorerBackend.Application.Connection
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
