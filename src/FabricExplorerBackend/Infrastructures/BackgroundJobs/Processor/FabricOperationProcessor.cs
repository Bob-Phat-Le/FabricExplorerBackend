using FabricExplorerBackend.Application.Notification;
using FabricExplorerBackend.Application.Operation;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.Infrastructures.Cache;
using FabricExplorerBackend.Infrastructures.Fabric.Dtos;
using FabricExplorerBackend.Infrastructures.Fabric.FabricRestClient;
using FabricExplorerBackend.Infrastructures.Options;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace FabricExplorerBackend.Infrastructures.Workers.Processor
{
    public sealed class FabricOperationProcessor(
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    IFabricRestClientFactory restClientFactory,
    ICacheService cache,
    IOptions<FabricOptions> fabricOptions,
    IOptions<OperationPollingOptions> operationPollingOptions,
    TimeProvider clock,
    ILogger<FabricOperationProcessor> logger) : IFabricOperationProcessor
    {
        private const int MaxMessageLength = 2000;
        private readonly OperationPollingOptions opt = operationPollingOptions.Value;
        private FabricOptions fabricOptions = fabricOptions.Value;

        public async Task PollAsync(Guid operationId, CancellationToken ct)
        {
            var repo = unitOfWork.FabricOperationRepository;
            var now = clock.GetUtcNow();

            if (!await repo.TryClaimAsync(operationId, now, opt.LeaseDuration, ct))
                return;

            var op = await repo.GetByIdAsync(operationId, trackChanges: true, cancellationToken: ct);
            if (op is null || op.Status.IsTerminal())
                return;

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
                throw;
            }
            catch (ExternalServiceException ex) when (ex.UpstreamStatus == 404)
            {
                logger.LogWarning(ex, "Fabric no longer knows this operation");
                Finish(op, FabricOperationStatus.Unknown, ErrorCodes.Operation.FabricLost,
                    "Fabric no longer has a record of this operation. The work may have completed.", now);
            }
            catch (Exception ex)
            {
                RecordPollFailure(op, ex, now);
            }

            // The processor stays the single source of truth for the final status; the notification only reports it.
            // It is staged before the save so the status change and its notification commit in one transaction.
            if (op.Status.IsTerminal())
                await notificationService.StageOperationResultAsync(op, CancellationToken.None);

            await unitOfWork.SaveChangesAsync();

            if (op.Status.IsTerminal())
                logger.LogInformation("Operation finished with {Status} ({ErrorCode})", op.Status, op.ErrorCode);

            if (op.Status == FabricOperationStatus.Succeeded)
                await OnSucceededAsync(op);
        }

        private async Task PollCoreAsync(FabricOperation op, DateTimeOffset now, CancellationToken ct)
        {
            if (now - op.CreatedAt > opt.MaxOperationAge)
            {
                Finish(op, FabricOperationStatus.TimedOut, ErrorCodes.Operation.TimedOut,
                    $"Fabric did not report completion within {opt.MaxOperationAge}.", now);
                return;
            }

            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(op.ConnectionId, cancellationToken: ct);
            if (connection is null)
            {
                Finish(op, FabricOperationStatus.Unknown, ErrorCodes.Connection.NotFound,
                    "The connection was deleted while the operation was running.", now);
                return;
            }

            //if (!IsTrustedFabricUrl(op.FabricOperationUrl, out var url))
            //{
            //    Finish(op, FabricOperationStatus.Failed, ErrorCodes.Operation.UntrustedUrl,
            //        "The stored operation URL does not point to Fabric.", now);
            //    return;
            //}

            var client = restClientFactory.CreateFabricRestClient(connection);
            var state = await client.GetAsync<FabricOperationStateDto>(op.FabricOperationUrl!, ct);

            Apply(op, state, now);
        }

        private void Apply(FabricOperation op, FabricOperationStateDto? state, DateTimeOffset now)
        {
            op.LastPolledAt = now;
            op.UpdatedAt = now;
            op.ConsecutivePollFailures = 0;
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

                default:
                    op.NextPollAt = now + NextDelay(op, now);
                    break;
            }
        }

        private static TimeSpan NextDelay(FabricOperation op, DateTimeOffset now)
        {
            var age = now - op.CreatedAt;
            return age < TimeSpan.FromMinutes(2) ? TimeSpan.FromSeconds(5)
                 : age < TimeSpan.FromMinutes(15) ? TimeSpan.FromSeconds(15)
                 : TimeSpan.FromSeconds(60);
        }

        private void RecordPollFailure(FabricOperation op, Exception ex, DateTimeOffset now)
        {
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
                Finish(op, FabricOperationStatus.Unknown, ErrorCodes.Operation.PollGaveUp,
                    $"Stopped polling after {op.ConsecutivePollFailures} consecutive failures. Last error: {failure.Code}.", now);
                return;
            }

            var seconds = Math.Min(opt.MaxBackoff.TotalSeconds,
                opt.BaseBackoff.TotalSeconds * Math.Pow(2, op.ConsecutivePollFailures - 1));
            op.NextPollAt = now + TimeSpan.FromSeconds(seconds);
        }


        private static void Finish(FabricOperation op, FabricOperationStatus status, string? errorCode, string? message, DateTimeOffset now)
        {
            op.Status = status;
            op.ErrorCode = errorCode;
            op.ErrorMessage = message is { Length: > MaxMessageLength } ? message[..MaxMessageLength] : message;
            op.CompletedAt = now;
            op.LastPolledAt = now;
            op.UpdatedAt = now;
            op.NextPollAt = null;
            op.LockedUntil = null;
        }

        private async Task OnSucceededAsync(FabricOperation op)
        {
            try
            {
                if (op is { OperationType: FabricOperationTypes.LoadTable, ResourceId: { } lakehouseId, ResourceName: { } table })
                {
                    var key = cache.CreateCacheKey(
                        "table-stats", op.ConnectionId.ToString(), op.WorkspaceId.ToString(), lakehouseId.ToString(), table);
                    await cache.RemoveAsync(key);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Cannot invalidate cache after {OperationType} succeeded", op.OperationType);
            }
        }

        private bool IsTrustedFabricUrl(string? value, out string url)
        {
            url = string.Empty;
            var baseUrl = fabricOptions.Api.BaseUrl;

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
