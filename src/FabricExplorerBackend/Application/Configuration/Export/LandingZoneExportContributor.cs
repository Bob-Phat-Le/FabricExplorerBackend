using FabricExplorerBackend.Application.Configuration.Documents;
using FabricExplorerBackend.Infrastructures.LandingZone;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using ConnectionEntity = FabricExplorerBackend.Domain.Entities.Connection;

namespace FabricExplorerBackend.Application.Configuration.Export
{
    /// <summary>Writes the Landing Zone URI of a connection. The mirrored database id is left out: it depends on the target workspace.</summary>
    public sealed class LandingZoneExportContributor(IUnitOfWork unitOfWork) : IConnectionExportContributor
    {
        public async Task ContributeAsync(ConnectionEntity connection, ConnectionEntry entry, ExportContext context, CancellationToken cancellationToken)
        {
            if (!context.Include.LandingZone)
                return;

            var config = await unitOfWork.LandingZoneRepository.GetConfigAsync(connection.Id, cancellationToken: cancellationToken);
            if (config is null)
                return;

            var location = new LandingZoneLocation(new Uri(config.StorageEndpoint), config.FileSystem, config.RootPath);
            entry.LandingZone = new LandingZoneEntry { Uri = location.ToUri() };
        }
    }
}
