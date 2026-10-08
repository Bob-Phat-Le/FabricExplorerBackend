using FabricExplorerBackend.Commons.Models.Responses.Fabric.MirroredDatabase;
using FabricExplorerBackend.Domain.Enums;
using FabricModel = Microsoft.Fabric.Api.MirroredDatabase.Models;

namespace FabricExplorerBackend.Features.Fabric.MirroredDatabase
{
    public class MirroredDatabaseMapper : IMirroredDatabaseMapper
    {
        public MirroredDatabaseDetailResponse Map(
            FabricModel.MirroredDatabase mirroredDatabase,
            Guid workspaceId,
            string workspaceName,
            MirroringStatus mirroringStatus,
            MirroringDefinitionInfo? definition)
        {
            return new MirroredDatabaseDetailResponse
            {
                MirroredDatabaseId = (Guid)mirroredDatabase.Id!,
                MirroredDatabaseName = mirroredDatabase.DisplayName,
                WorkspaceId = workspaceId,
                WorkspaceName = workspaceName,
                SourceType = definition?.SourceType,
                SourceName = definition?.SourceName,
                // Fabric không cung cấp ngày tạo cho mirrored database --> Fabric UI có cung cấp created date --> cần kiểm tra lại
                CreatedAt = null,
                MirroringStatus = mirroringStatus,
                Status = ToOnlineStatus(mirroringStatus)
            };
        }

        public MirroringDatabaseStatusResponse Map(FabricModel.MirroringStatusResponse mirroringStatusResponse)
        {
            return new MirroringDatabaseStatusResponse
            {
                Status = MapStatus(mirroringStatusResponse.Status)
            };
        }

        // Status của SDK là extensible enum nên parse theo chuỗi; giá trị lạ -> Unknown thay vì throw
        public MirroringStatus MapStatus(object? fabricStatus)
            => Enum.TryParse<MirroringStatus>(fabricStatus?.ToString(), ignoreCase: true, out var status)
                ? status
                : MirroringStatus.Unknown;

        // Active = đang chạy; Starting/Initializing/Stopping/Stopped/Paused/Initialized đều chưa đồng bộ nên coi là Offline
        public MirroredDatabaseOnlineStatus ToOnlineStatus(MirroringStatus status) => status switch
        {
            MirroringStatus.Running => MirroredDatabaseOnlineStatus.Active,
            MirroringStatus.Unknown => MirroredDatabaseOnlineStatus.Unknown,
            _ => MirroredDatabaseOnlineStatus.Offline
        };

        public TableMirroringStatus MapTableStatus(object? fabricStatus)
            => Enum.TryParse<TableMirroringStatus>(fabricStatus?.ToString(), ignoreCase: true, out var status)
                ? status
                : TableMirroringStatus.Unknown;

        public TableMirroringStatusResponse MapTable(
            string? sourceSchema,
            string? sourceTable,
            object? status,
            DateTimeOffset? lastSync,
            int? lagInSeconds,
            long processedRows,
            MirroringDefinitionInfo? definition)
        {
            var table = sourceTable ?? string.Empty;
            var targetSchema = !string.IsNullOrWhiteSpace(definition?.DefaultSchema) ? definition!.DefaultSchema : sourceSchema;

            return new TableMirroringStatusResponse
            {
                TableName = table,
                Source = Qualify(sourceSchema, table),
                Target = Qualify(targetSchema, table),
                Status = MapTableStatus(status),
                LastSync = lastSync,
                Lag = lagInSeconds,
                ProcessedRows = processedRows
            };
        }

        private static string Qualify(string? schema, string table)
            => string.IsNullOrWhiteSpace(schema) ? table : $"{schema}.{table}";
    }
}
