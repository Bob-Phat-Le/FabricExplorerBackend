using FabricExplorerBackend.Application.LandingZone.Contracts;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Infrastructures.LandingZone;

namespace FabricExplorerBackend.Infrastructures.Helpers.HelperConcretes.LandingZone
{
    public interface ILandingZoneHelper : IBaseHelper
    {
        Result<LandingZoneLocation> BuildLocation(Domain.Entities.Connection connection, ConfigureLandingZoneRequest request);
        Task<Result<Session>> OpenAsync(Guid connectionId, CancellationToken cancellationToken);
        Task<Error?> EnsureFolderExistsAsync(Session session, string folder, CancellationToken cancellationToken);
        Task<Stream> CreateArchiveAsync(Session session, IReadOnlyList<string> paths, CancellationToken cancellationToken);
    }
}
