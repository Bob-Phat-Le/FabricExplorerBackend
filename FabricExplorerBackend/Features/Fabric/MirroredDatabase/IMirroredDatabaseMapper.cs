using FabricExplorerBackend.Commons.Models.Responses.Fabric.MirroredDatabase;
using FabricExplorerBackend.Domain.Enums;
using FabricModel = Microsoft.Fabric.Api.MirroredDatabase.Models;

namespace FabricExplorerBackend.Features.Fabric.MirroredDatabase
{
    public interface IMirroredDatabaseMapper
    {
        MirroredDatabaseDetailResponse Map(
            FabricModel.MirroredDatabase mirroredDatabase,
            Guid workspaceId,
            string workspaceName,
            MirroringStatus mirroringStatus,
            MirroringDefinitionInfo? definition);
        MirroringDatabaseStatusResponse Map(FabricModel.MirroringStatusResponse mirroringStatusResponse);
        MirroringStatus MapStatus(object? fabricStatus);
        MirroredDatabaseOnlineStatus ToOnlineStatus(MirroringStatus status);
        TableMirroringStatus MapTableStatus(object? fabricStatus);
        TableMirroringStatusResponse MapTable(
            string? sourceSchema,
            string? sourceTable,
            object? status,
            DateTimeOffset? lastSync,
            int? lagInSeconds,
            long processedRows,
            MirroringDefinitionInfo? definition);
    }
}
