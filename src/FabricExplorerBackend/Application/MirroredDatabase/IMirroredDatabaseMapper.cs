using FabricExplorerBackend.Application.MirroredDatabase.Contracts;
using FabricModel = Microsoft.Fabric.Api.MirroredDatabase.Models;

namespace FabricExplorerBackend.Application.MirroredDatabase
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
