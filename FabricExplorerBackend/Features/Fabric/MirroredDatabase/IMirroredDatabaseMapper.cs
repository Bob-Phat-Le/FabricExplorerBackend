using FabricExplorerBackend.Commons.Models.Responses.Fabric.MirroredDatabase;
using Microsoft.Fabric.Api.MirroredDatabase.Models;
using FabricModel = Microsoft.Fabric.Api.MirroredDatabase.Models;

namespace FabricExplorerBackend.Features.Fabric.MirroredDatabase
{
    public interface IMirroredDatabaseMapper
    {
        MirroredDatabaseDetailResponse Map(FabricModel.MirroredDatabase mirroredDatabase);
        MirroringDatabaseStatusResponse Map(MirroringStatusResponse mirroringStatusResponse);
    }
}
