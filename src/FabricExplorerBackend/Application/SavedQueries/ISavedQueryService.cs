using FabricExplorerBackend.Application.SavedQueries.Contracts;
using FabricExplorerBackend.Commons.Contracts;
using FabricExplorerBackend.Commons.Paginations;
using FabricExplorerBackend.Commons.Results;

namespace FabricExplorerBackend.Application.SavedQueries
{
    public interface ISavedQueryService
    {
        Task<Result<PagedResponse<IEnumerable<SavedQueryResponse>>>> GetAllAsync(ListSavedQueriesRequest request, CancellationToken cancellationToken = default);
        Task<Result<SavedQueryResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Result<SavedQueryResponse>> CreateAsync(CreateSavedQueryRequest request, CancellationToken cancellationToken = default);
        Task<Result<SavedQueryResponse>> UpdateAsync(Guid id, UpdateSavedQueryRequest request, CancellationToken cancellationToken = default);
        Task<Result<ConfirmationResponse>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
