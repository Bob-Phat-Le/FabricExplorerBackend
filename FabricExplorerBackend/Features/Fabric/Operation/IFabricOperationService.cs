using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Requests.Fabric.FabricOperation;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricOperation;

namespace FabricExplorerBackend.Features.Fabric.Operation
{
    public interface IFabricOperationService
    {
        /// <summary>Lưu operation vừa được Fabric chấp nhận (HTTP 202) để theo dõi.</summary>
        Task<Result<FabricOperationResponse>> CreateFabricOperationAsync(CreateFabricOperationRequest request);

        /// <summary>
        /// Trả trạng thái operation đang lưu trong DB cho frontend. Hàm này không gọi Fabric;
        /// việc cập nhật trạng thái do worker đảm nhiệm thông qua <see cref="RefreshAsync"/>.
        /// </summary>
        Task<Result<FabricOperationResponse>> GetFabricOperationAsync(
            Guid connectionId, Guid operationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Hỏi Fabric về trạng thái của một operation và lưu kết quả vào DB.
        /// Bỏ qua nếu operation đã xong hoặc chưa đến NextPollAt. Dùng bởi worker.
        /// </summary>
        Task RefreshAsync(Guid operationId, CancellationToken cancellationToken = default);
    }
}
