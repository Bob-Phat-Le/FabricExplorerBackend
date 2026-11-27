using FabricExplorerBackend.Application.Configuration.Documents;
using FabricExplorerBackend.Application.LandingZone.Contracts.Explorers;
using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Infrastructures.Helpers.HelperConcretes.LandingZone;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using ConnectionEntity = FabricExplorerBackend.Domain.Entities.Connection;

namespace FabricExplorerBackend.Application.Configuration.Import
{
    /// <summary>Something done to a connection right after it was created or updated, saved together with it.</summary>
    public interface IConnectionImportStep
    {
        /// <returns>Flags to show on the result of the connection.</returns>
        Task<IReadOnlyList<string>> ApplyAsync(ConnectionEntity connection, ConnectionEntry entry, ImportSession session, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Restores the Landing Zone location. It is stored without reaching the storage: a freshly
    /// imported connection has no secret yet, and the location is checked when it is first used.
    /// </summary>
    public sealed class LandingZoneImportStep(
        IUnitOfWork unitOfWork,
        ILandingZoneHelper landingZoneHelper,
        TimeProvider clock) : IConnectionImportStep
    {
        public const string Restored = "LandingZoneRestored";

        public async Task<IReadOnlyList<string>> ApplyAsync(
            ConnectionEntity connection, ConnectionEntry entry, ImportSession session, CancellationToken cancellationToken)
        {
            if (!session.Options.RestoreLandingZone || string.IsNullOrWhiteSpace(entry.LandingZone?.Uri))
                return [];

            var parsed = landingZoneHelper.BuildLocation(connection, new ConfigureLandingZoneRequest(null, entry.LandingZone!.Uri));
            if (!parsed.IsSuccess)
                return [];

            var location = parsed.Data!;
            var now = clock.GetUtcNow();
            var config = await unitOfWork.LandingZoneRepository.GetConfigAsync(connection.Id, trackChanges: true, cancellationToken);

            if (config is null)
            {
                await unitOfWork.LandingZoneRepository.AddConfigAsync(new LandingZoneConfig
                {
                    Id = Guid.NewGuid(),
                    ConnectionId = connection.Id,
                    StorageEndpoint = location.Endpoint.GetLeftPart(UriPartial.Authority),
                    FileSystem = location.FileSystem,
                    RootPath = location.RootPath,
                    CreatedAt = now
                }, cancellationToken);
            }
            else
            {
                config.StorageEndpoint = location.Endpoint.GetLeftPart(UriPartial.Authority);
                config.FileSystem = location.FileSystem;
                config.RootPath = location.RootPath;
                config.UpdatedAt = now;
                unitOfWork.LandingZoneRepository.UpdateConfig(config);
            }

            return [Restored];
        }
    }
}
