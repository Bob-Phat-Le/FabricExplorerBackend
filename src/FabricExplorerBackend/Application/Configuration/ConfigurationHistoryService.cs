using FabricExplorerBackend.Application.Configuration.Contracts;
using FabricExplorerBackend.Application.Configuration.Documents;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Commons.Security;
using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Infrastructures.Options;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FabricExplorerBackend.Application.Configuration
{
    public sealed class ConfigurationHistoryService(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        TimeProvider clock,
        IOptions<ConfigurationTransferOptions> options) : IConfigurationHistoryService
    {
        public const int DefaultLimit = 20;
        public const int MaxLimit = 100;

        public async Task RecordAsync(
            Guid userId, string kind, string status, string? summary, int itemCount,
            string? idempotencyKey, ImportResponse? response, CancellationToken cancellationToken)
        {
            var resultJson = response is null ? null : JsonSerializer.Serialize(response, ConfigurationJson.Response);

            await unitOfWork.ConfigurationOperationRepository.AddAsync(
                ConfigurationOperation.Create(userId, kind, status, summary, itemCount, idempotencyKey, resultJson, clock.GetUtcNow()),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await unitOfWork.ConfigurationOperationRepository.TrimAsync(userId, options.Value.HistoryRetention, cancellationToken);
        }

        public async Task<ImportResponse?> FindImportReplayAsync(Guid userId, string idempotencyKey, CancellationToken cancellationToken)
        {
            var operation = await unitOfWork.ConfigurationOperationRepository.FindByIdempotencyKeyAsync(userId, idempotencyKey, cancellationToken);

            return operation?.ResultJson is null
                ? null
                : JsonSerializer.Deserialize<ImportResponse>(operation.ResultJson, ConfigurationJson.Response);
        }

        public async Task<Result<IReadOnlyList<ConfigurationHistoryItem>>> GetRecentAsync(int? limit, CancellationToken cancellationToken)
        {
            var take = Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);
            var operations = await unitOfWork.ConfigurationOperationRepository.GetRecentAsync(currentUser.Id, take, cancellationToken);

            IReadOnlyList<ConfigurationHistoryItem> items = operations
                .Select(o => new ConfigurationHistoryItem(o.Id, o.Kind, o.Status, o.Summary, o.ItemCount, o.CreatedAt))
                .ToList();

            return Result<IReadOnlyList<ConfigurationHistoryItem>>.Success(items);
        }
    }
}
