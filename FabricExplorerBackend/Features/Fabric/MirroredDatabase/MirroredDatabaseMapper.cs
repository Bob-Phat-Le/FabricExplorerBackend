using FabricExplorerBackend.Commons.Models.Responses.Fabric.MirroredDatabase;
using Microsoft.Fabric.Api.MirroredDatabase.Models;
using Riok.Mapperly.Abstractions;
using FabricModel = Microsoft.Fabric.Api.MirroredDatabase.Models;

namespace FabricExplorerBackend.Features.Fabric.MirroredDatabase
{
    [Mapper]
    public partial class MirroredDatabaseMapper : IMirroredDatabaseMapper
    {
        [MapProperty(nameof(FabricModel.MirroredDatabase.Id),nameof(MirroredDatabaseDetailResponse.MirroredDatabaseId))]
        [MapProperty(nameof(FabricModel.MirroredDatabase.DisplayName),nameof(MirroredDatabaseDetailResponse.MirroredDatabaseName))]
        [MapProperty(nameof(FabricModel.MirroredDatabase.WorkspaceId),nameof(MirroredDatabaseDetailResponse.WorkspaceId))]
        [MapProperty(nameof(FabricModel.MirroredDatabase.Type),nameof(MirroredDatabaseDetailResponse.Source))]
        public partial MirroredDatabaseDetailResponse Map(FabricModel.MirroredDatabase mirroredDatabase);

        public MirroringDatabaseStatusResponse Map(MirroringStatusResponse mirroringStatusResponse)
        {
            return new MirroringDatabaseStatusResponse
            {
                Status = mirroringStatusResponse.Status switch
                {
                    var status when status == FabricModel.MirroringStatus.Initializing => Domain.Enums.MirroringStatus.Initializing,
                    var status when status == FabricModel.MirroringStatus.Initialized => Domain.Enums.MirroringStatus.Initialized,
                    var status when status == FabricModel.MirroringStatus.Paused => Domain.Enums.MirroringStatus.Paused,
                    var status when status == FabricModel.MirroringStatus.Running => Domain.Enums.MirroringStatus.Running,
                    var status when status == FabricModel.MirroringStatus.Starting => Domain.Enums.MirroringStatus.Starting,
                    var status when status == FabricModel.MirroringStatus.Stopped => Domain.Enums.MirroringStatus.Stopped,
                    var status when status == FabricModel.MirroringStatus.Stopping => Domain.Enums.MirroringStatus.Stopping,

                    _ => throw new Exception()
                }
            };
        }
    }
}
