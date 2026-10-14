using FabricExplorerBackend.Application.Operation;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.Infrastructures.BackgroundJobs;
using FabricExplorerBackend.Infrastructures.Cache;
using FabricExplorerBackend.Infrastructures.Fabric.Dtos;
using FabricExplorerBackend.Infrastructures.Fabric.FabricRestClient;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace FabricExplorerBackend.Infrastructures.Workers.Processor
{
    public sealed class FabricOperationProcessor(
    IUnitOfWork unitOfWork,
    IFabricRestClientFactory restClientFactory,
    ICacheService cache,
    IConfiguration configuration,
    IOptions<OperationPollingOptions> options,
    TimeProvider clock,
    ILogger<FabricOperationProcessor> logger) : IFabricOperationProcessor
    {
        private const int MaxMessageLength = 2000;
        private readonly OperationPollingOptions opt = options.Value;

        public async Task PollAsync(Guid operationId, CancellationToken ct)
        {
            var repo = unitOfWork.FabricOperationRepository;
            var now = clock.GetUtcNow();

            // Nhiều instance cùng chạy thì chỉ một instance giữ được dòng này.
            if (!await repo.TryClaimAsync(operationId, now, opt.LeaseDuration, ct))
                return;

            var op = await repo.GetByIdAsync(operationId, trackChanges: true, cancellationToken: ct);
            if (op is null || op.Status.IsTerminal())
                return;

            // Mọi dòng log trong lần poll này đều mang các property bên dưới.
            using var logScope = logger.BeginScope(new Dictionary<string, object?>
            {
                ["OperationId"] = op.Id,
                ["FabricOperationId"] = op.FabricOperationId,
                ["OperationType"] = op.OperationType,
                ["ConnectionId"] = op.ConnectionId,
                ["WorkspaceId"] = op.WorkspaceId
            });

            try
            {
                await PollCoreAsync(op, now, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;   // Ứng dụng đang tắt: không lưu gì, lease sẽ tự hết hạn và dòng được poll lại sau.
            }
            catch (ExternalServiceException ex) when (ex.UpstreamStatus == 404)
            {
                // Fabric không còn biết operation này (hết hạn, bị xoá). Chưa chắc nó thất bại nên KHÔNG đặt Failed.
                logger.LogWarning(ex, "Fabric no longer knows this operation");
                Finish(op, FabricOperationStatus.Unknown, ErrorCodes.Operation.FabricLost,
                    "Fabric no longer has a record of this operation. The work may have completed.", now);
            }
            catch (Exception ex)
            {
                RecordPollFailure(op, ex, now);
            }

            await unitOfWork.SaveChangesAsync();

            if (op.Status.IsTerminal())
                logger.LogInformation("Operation finished with {Status} ({ErrorCode})", op.Status, op.ErrorCode);

            // Chạy SAU khi đã lưu trạng thái: lỗi ở bước phụ này không được làm mất kết quả operation.
            if (op.Status == FabricOperationStatus.Succeeded)
                await OnSucceededAsync(op);
        }

        private async Task PollCoreAsync(FabricOperation op, DateTimeOffset now, CancellationToken ct)
        {
            // 1. Chặn operation bị kẹt vĩnh viễn.
            if (now - op.CreatedAt > opt.MaxOperationAge)
            {
                Finish(op, FabricOperationStatus.TimedOut, ErrorCodes.Operation.TimedOut,
                    $"Fabric did not report completion within {opt.MaxOperationAge}.", now);
                return;
            }

            // 2. Connection có thể đã bị xoá trong lúc operation chạy.
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(op.ConnectionId, cancellationToken: ct);
            if (connection is null)
            {
                Finish(op, FabricOperationStatus.Unknown, ErrorCodes.Connection.NotFound,
                    "The connection was deleted while the operation was running.", now);
                return;
            }

            // 3. Không gửi bearer token tới URL không phải của Fabric (URL lấy từ DB).
            if (!IsTrustedFabricUrl(op.FabricOperationUrl, out var url))
            {
                Finish(op, FabricOperationStatus.Failed, ErrorCodes.Operation.UntrustedUrl,
                    "The stored operation URL does not point to Fabric.", now);
                return;
            }

            // 4. Hỏi Fabric. FabricRestClient ném ExternalServiceException nếu Fabric trả lỗi hoặc không phản hồi.
            var client = restClientFactory.CreateFabricRestClient(connection);
            var state = await client.GetAsync<FabricOperationStateDto>(url, ct);

            Apply(op, state, now);
        }

        private void Apply(FabricOperation op, FabricOperationStateDto? state, DateTimeOffset now)
        {
            op.LastPolledAt = now;
            op.UpdatedAt = now;
            op.ConsecutivePollFailures = 0;   // poll này thành công
            op.LockedUntil = null;
            if (state?.PercentComplete is { } percent)
                op.PercentComplete = percent;

            switch (state?.Status?.ToLowerInvariant())
            {
                case "succeeded":
                    Finish(op, FabricOperationStatus.Succeeded, null, null, now);
                    op.PercentComplete = 100;
                    break;

                case "failed":
                    // Đây mới là operation thất bại thật: lưu nguyên mã lỗi và thông điệp Fabric trả về.
                    Finish(op, FabricOperationStatus.Failed,
                        state.Error?.ErrorCode ?? ErrorCodes.Operation.FabricFailed, state.Error?.Message, now);
                    break;

                case "running":
                    op.Status = FabricOperationStatus.Running;
                    op.NextPollAt = now + NextDelay(op, now);
                    break;

                case "notstarted":
                    op.Status = FabricOperationStatus.Pending;
                    op.NextPollAt = now + NextDelay(op, now);
                    break;

                default:   // "undefined" hoặc thiếu: chưa kết luận được, coi như chưa xong và giữ nguyên Status
                    op.NextPollAt = now + NextDelay(op, now);
                    break;
            }
        }

        // Mới tạo thì poll dày, càng lâu càng thưa để giảm tải cho Fabric.
        // Có thể thay bằng header Retry-After của response poll nếu FabricRestClient trả ra header.
        private static TimeSpan NextDelay(FabricOperation op, DateTimeOffset now)
        {
            var age = now - op.CreatedAt;
            return age < TimeSpan.FromMinutes(2) ? TimeSpan.FromSeconds(5)
                 : age < TimeSpan.FromMinutes(15) ? TimeSpan.FromSeconds(15)
                 : TimeSpan.FromSeconds(60);
        }

        // ------------------------------------------------------------------ Lỗi khi poll

        private void RecordPollFailure(FabricOperation op, Exception ex, DateTimeOffset now)
        {
            // Dùng chung bộ phân loại với GlobalExceptionHandler để log đồng nhất (mã lỗi, mức log, có stack hay không).
            var failure = ExceptionClassifier.Classify(ex);

            op.ConsecutivePollFailures++;
            op.LastPolledAt = now;
            op.UpdatedAt = now;
            op.LockedUntil = null;

            logger.Log(failure.Level, failure.LogStackTrace ? ex : null,
                "Polling failed ({Failures}/{Max}) {ErrorCode}: {Message}",
                op.ConsecutivePollFailures, opt.MaxConsecutivePollFailures, failure.Code, ex.Message);

            if (op.ConsecutivePollFailures >= opt.MaxConsecutivePollFailures)
            {
                // Bỏ cuộc nhưng KHÔNG kết luận Fabric thất bại: ta chỉ mất dấu.
                Finish(op, FabricOperationStatus.Unknown, ErrorCodes.Operation.PollGaveUp,
                    $"Stopped polling after {op.ConsecutivePollFailures} consecutive failures. Last error: {failure.Code}.", now);
                return;
            }

            // Backoff luỹ thừa: 10s, 20s, 40s, 80s, rồi chặn ở MaxBackoff. Trạng thái Pending/Running giữ nguyên.
            var seconds = Math.Min(opt.MaxBackoff.TotalSeconds,
                opt.BaseBackoff.TotalSeconds * Math.Pow(2, op.ConsecutivePollFailures - 1));
            op.NextPollAt = now + TimeSpan.FromSeconds(seconds);
        }

        // ------------------------------------------------------------------ Kết thúc

        private static void Finish(FabricOperation op, FabricOperationStatus status, string? errorCode, string? message, DateTimeOffset now)
        {
            op.Status = status;
            op.ErrorCode = errorCode;
            // Cắt ngắn để không vượt độ dài cột: SaveChanges lỗi vì một message quá dài sẽ làm dòng này kẹt mãi.
            op.ErrorMessage = message is { Length: > MaxMessageLength } ? message[..MaxMessageLength] : message;
            op.CompletedAt = now;
            op.LastPolledAt = now;
            op.UpdatedAt = now;
            op.NextPollAt = null;
            op.LockedUntil = null;
        }

        // Việc cần làm khi operation thành công, tuỳ theo loại. LoadTable: xoá cache stats của đúng bảng đó,
        // nếu không FE sẽ thấy RowCount/LastModified cũ trong tối đa TableStatsCacheSeconds sau khi load xong.
        private async Task OnSucceededAsync(FabricOperation op)
        {
            try
            {
                if (op is { OperationType: FabricOperationTypes.LoadTable, ResourceId: { } lakehouseId, ResourceName: { } table })
                {
                    // Phải KHỚP công thức key trong LakehouseTableStatsService ("table-stats", connection, workspace, lakehouse, table).
                    // Nên đưa công thức này vào một chỗ dùng chung để hai nơi không bị lệch.
                    var key = cache.CreateCacheKey(
                        "table-stats", op.ConnectionId.ToString(), op.WorkspaceId.ToString(), lakehouseId.ToString(), table);
                    await cache.RemoveAsync(key);   // cần thêm RemoveAsync vào ICacheService (xem README)
                }
            }
            catch (Exception ex)
            {
                // Nuốt có chủ đích (cache hỏng không được làm hỏng kết quả operation đã lưu) nên phải log.
                logger.LogWarning(ex, "Cannot invalidate cache after {OperationType} succeeded", op.OperationType);
            }
        }

        // ------------------------------------------------------------------ Bảo mật

        // Token của connection chỉ được gửi tới đúng host Fabric đã cấu hình.
        private bool IsTrustedFabricUrl(string? value, out string url)
        {
            url = string.Empty;
            var baseUrl = configuration["External:BaseFabricApiUrl"];

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
                || !Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri)
                || uri.Scheme != Uri.UriSchemeHttps
                || !uri.Host.Equals(baseUri.Host, StringComparison.OrdinalIgnoreCase))
                return false;

            url = uri.ToString();
            return true;
        }
    }
}
