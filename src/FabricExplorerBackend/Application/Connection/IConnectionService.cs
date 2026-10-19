using FabricExplorerBackend.Application.Connection.Contracts;
using FabricExplorerBackend.Commons.Contracts;
using FabricExplorerBackend.Commons.Paginations;
using FabricExplorerBackend.Commons.Results;

namespace FabricExplorerBackend.Application.Connection
{
    public interface IConnectionService
    {
        Task<Result<ConfirmationResponse>> CreateConnectionAsync(CreateConnectionRequest request, CancellationToken cancellationToken = default);
        Task<Result<PagedResponse<IEnumerable<ConnectionResponse>>>> GetAllConnectionsAsync(PaginationRequest? paginationRequest, CancellationToken cancellationToken = default);
        Task<Result<ConnectionResponse>> GetConnectionByIdAsync(Guid connectionId, CancellationToken cancellationToken = default);
        Task<Result<ConnectionResponse>> GetActiveConnectionAsync(CancellationToken cancellationToken = default);
        Task<Result<ConfirmationResponse>> UpdateConnection(Guid connectionId, UpdateConnectionRequest request, CancellationToken cancellationToken = default);
        Task<Result<ConfirmationResponse>> DeleteConnection(Guid connectionId, CancellationToken cancellationToken = default);
        Task<Result<ConfirmationResponse>> TestConnection(Guid connectionId, CancellationToken cancellationToken = default);
        Task<Result<ConfirmationResponse>> ActiveConnection(Guid targetConnectionId, CancellationToken cancellationToken = default);
    }
}
