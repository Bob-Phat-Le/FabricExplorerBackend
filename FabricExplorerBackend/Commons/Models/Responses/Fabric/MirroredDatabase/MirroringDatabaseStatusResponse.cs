using FabricExplorerBackend.Domain.Enums;

namespace FabricExplorerBackend.Commons.Models.Responses.Fabric.MirroredDatabase
{
    public class MirroringDatabaseStatusResponse
    {
        public DateTimeOffset? LastSynchronization { get; set; }
        public long RecordSynchronized { get; set; }
        public int? CurrentLatency { get; set; }
        public MirroringStatus Status { get; set; }
    }
}
