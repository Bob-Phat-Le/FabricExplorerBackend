using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Features.Cache;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using System.Text.Json;

namespace FabricExplorerBackend.Helpers
{
    public class CredentialHelper(
        ICacheService cacheService,
        IUnitOfWork unitOfWork) : ICredentialHelper
    {
        public async Task<Connection?> GetCredentialWithConnectionId(Guid connectionId)
        {
            var cacheKey = cacheService.CreateCacheKey(connectionId.ToString());

            var connectionAsString = (await cacheService.GetValueAsync(cacheKey))?.ToString();
            if (!string.IsNullOrEmpty(connectionAsString))
                return JsonSerializer.Deserialize<Connection>(connectionAsString);

            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId);
            await cacheService.SetValueAsync(cacheKey, JsonSerializer.Serialize(connection));

            return connection;
        }
    }
}
