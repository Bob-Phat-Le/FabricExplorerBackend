using FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces
{
    public interface ILandingZoneRepository
    {
        Task<LandingZoneConfig?> GetConfigAsync(Guid connectionId, bool trackChanges = false, CancellationToken cancellationToken = default);
        Task AddConfigAsync(LandingZoneConfig config, CancellationToken cancellationToken = default);
        void UpdateConfig(LandingZoneConfig config);
        void DeleteConfig(LandingZoneConfig config);

        Task AddOperationsAsync(IEnumerable<LandingZoneOperation> operations, CancellationToken cancellationToken = default);
        Task<List<LandingZoneOperation>> GetOperationsAsync(Guid connectionId, int take, CancellationToken cancellationToken = default);

        /// <summary>Keeps only the newest <paramref name="keep"/> operations of the connection.</summary>
        Task TrimOperationsAsync(Guid connectionId, int keep, CancellationToken cancellationToken = default);
    }
}
