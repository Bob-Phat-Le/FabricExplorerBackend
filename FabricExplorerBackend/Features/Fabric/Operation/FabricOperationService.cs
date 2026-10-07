using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Requests.Fabric.FabricOperation;
using FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricOperation;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.Features.Fabric.FabricRestClient;
using FabricExplorerBackend.Helpers;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using System.Net;

namespace FabricExplorerBackend.Features.Fabric.Operation
{
    public class FabricOperationService(
        IUnitOfWork unitOfWork,
        IFabricOperationMapper mapper,
        IFabricRestClientFactory fabricRestClientFactory,
        IFabricLroHelper lroHelper,
        IConfiguration configuration,
        ILogger<FabricOperationService> logger) : IFabricOperationService
    {
        public async Task<Result<FabricOperationResponse>> CreateFabricOperationAsync(CreateFabricOperationRequest request)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;

                var operation = mapper.Map(request);
                operation.Status = FabricOperationStatus.Pending;
                operation.CreatedAt = now;
                operation.NextPollAt = lroHelper.GetNextPollAt(now, request.RetryAfter);

                await unitOfWork.FabricOperationRepository.AddAsync(operation);
                var lineChanges = await unitOfWork.SaveChangesAsync();
                if (lineChanges <= 0)
                    return Result<FabricOperationResponse>.Failure(ResultStatus.InternalError, "Failed to create fabric operation.");

                // EF Core đã sinh Id cho operation sau khi lưu
                return Result<FabricOperationResponse>.Success(mapper.Map(operation));
            }
            catch (Exception ex)
            {
                return Result<FabricOperationResponse>.Failure(ResultStatus.InternalError, ex.Message);
            }
        }

        public async Task<Result<FabricOperationResponse>> GetFabricOperationAsync(
            Guid connectionId,
            Guid operationId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var operation = await unitOfWork.FabricOperationRepository.GetByIdAsync(operationId);

                // Không phân biệt "không tồn tại" và "thuộc connection khác" để không lộ thông tin
                if (operation == null || operation.ConnectionId != connectionId)
                    return Result<FabricOperationResponse>.Failure(ResultStatus.NotFound, "Fabric operation not found.");

                // Chỉ đọc trạng thái đã lưu; worker mới là nơi hỏi Fabric
                return Result<FabricOperationResponse>.Success(mapper.Map(operation));
            }
            catch (Exception ex)
            {
                return Result<FabricOperationResponse>.Failure(ResultStatus.InternalError, ex.Message);
            }
        }

        public async Task RefreshAsync(Guid operationId, CancellationToken cancellationToken = default)
        {
            // Cần tracking để các thay đổi trên entity được SaveChanges ghi lại
            var operation = await unitOfWork.FabricOperationRepository.GetByIdAsync(operationId, trackChanges: true);
            if (operation == null)
                return;

            await RefreshIfDueAsync(operation, cancellationToken);
        }

        /// <summary>
        /// Hỏi Fabric về trạng thái operation khi operation chưa xong và đã đến NextPollAt.
        /// Kiểm tra lại NextPollAt ở đây để tránh hỏi thừa nếu trạng thái đã được cập nhật trong lúc chờ.
        /// </summary>
        private async Task RefreshIfDueAsync(Domain.Entities.FabricOperation operation, CancellationToken ct)
        {
            if (lroHelper.IsDone(operation.Status))
                return;

            var now = DateTimeOffset.UtcNow;
            if (operation.NextPollAt is { } nextPollAt && nextPollAt > now)
                return;

            if (lroHelper.IsExpired(operation.CreatedAt, now))
            {
                MarkFailed(operation, now, "OperationTimeout", "Operation did not finish within the allowed time.");
                await unitOfWork.SaveChangesAsync();
                return;
            }

            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(operation.ConnectionId);
            if (connection == null)
            {
                MarkFailed(operation, now, "ConnectionNotFound", "The connection that started this operation no longer exists.");
                await unitOfWork.SaveChangesAsync();
                return;
            }

            try
            {
                var client = fabricRestClientFactory.CreateFabricRestClient(connection);
                var url =
                    $"{configuration.GetValue<string>("External:BaseFabricApiUrl")}" +
                    $"/operations/{operation.FabricOperationId}";

                var state = await client.GetAsync<FabricOperationStateResponse>(url, ct);

                operation.Status = lroHelper.MapStatus(state?.Status);
                operation.PercentComplete = operation.Status == FabricOperationStatus.Succeeded
                    ? 100
                    : state?.PercentComplete;

                if (operation.Status == FabricOperationStatus.Failed)
                {
                    operation.ErrorCode = state?.Error?.ErrorCode;
                    operation.ErrorMessage = state?.Error?.Message;
                }

                operation.NextPollAt = lroHelper.IsDone(operation.Status)
                    ? null
                    : lroHelper.GetNextPollAt(now);
                operation.LastPolledAt = now;
                operation.UpdatedAt = now;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Fabric chỉ giữ thông tin operation trong một khoảng thời gian
                MarkFailed(operation, now, "OperationNotFound", "Fabric no longer has this operation.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Lỗi tạm thời (mạng, 429, 5xx...): giữ nguyên trạng thái và thử lại ở lần poll sau
                logger.LogWarning(ex, "Failed to poll Fabric operation {FabricOperationId}.", operation.FabricOperationId);
                operation.NextPollAt = lroHelper.GetNextPollAt(now);
                operation.LastPolledAt = now;
                operation.UpdatedAt = now;
            }

            await unitOfWork.SaveChangesAsync();
        }

        private static void MarkFailed(
            Domain.Entities.FabricOperation operation, DateTimeOffset now, string errorCode, string errorMessage)
        {
            operation.Status = FabricOperationStatus.Failed;
            operation.ErrorCode = errorCode;
            operation.ErrorMessage = errorMessage;
            operation.NextPollAt = null;
            operation.LastPolledAt = now;
            operation.UpdatedAt = now;
        }
    }
}
