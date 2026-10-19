using Azure;
using Microsoft.Fabric.Api;
using Microsoft.Fabric.Api.Core;
using Microsoft.Fabric.Api.Lakehouse;
using Microsoft.Fabric.Api.MirroredDatabase;
using Moq;
using System.Reflection;
using System.Runtime.CompilerServices;
using CoreModels = Microsoft.Fabric.Api.Core.Models;
using LakehouseModels = Microsoft.Fabric.Api.Lakehouse.Models;
using MirroredDbModels = Microsoft.Fabric.Api.MirroredDatabase.Models;
using WarehouseModels = Microsoft.Fabric.Api.Warehouse.Models;

namespace FabricExplorerBackend.UnitTests.Common
{
    public static class FabricTestHelper
    {
        public static void SetField<T>(T obj, string fieldName, object? value)
        {
            if (obj == null) return;
            var type = obj.GetType();
            while (type != null && type != typeof(object))
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(obj, value);
                    return;
                }

                var backing = type.GetField($"<{fieldName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                if (backing != null)
                {
                    backing.SetValue(obj, value);
                    return;
                }

                var prop = type.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(obj, value);
                    return;
                }

                type = type.BaseType;
            }
        }

        public static CoreModels.Workspace CreateWorkspace(Guid id, string displayName, string? capacityId = null)
        {
            var workspace = (CoreModels.Workspace)RuntimeHelpers.GetUninitializedObject(typeof(CoreModels.Workspace));
            SetField(workspace, "Id", (Guid?)id);
            SetField(workspace, "DisplayName", displayName);
            if (capacityId != null && Guid.TryParse(capacityId, out var capGuid))
                SetField(workspace, "CapacityId", (Guid?)capGuid);
            return workspace;
        }

        public static CoreModels.WorkspaceInfo CreateWorkspaceInfo(Guid id, string displayName)
        {
            var info = (CoreModels.WorkspaceInfo)RuntimeHelpers.GetUninitializedObject(typeof(CoreModels.WorkspaceInfo));
            SetField(info, "Id", (Guid?)id);
            SetField(info, "DisplayName", displayName);
            return info;
        }

        public static WarehouseModels.Warehouse CreateWarehouse(
            Guid id,
            string displayName,
            string? description = null,
            string? connectionString = null,
            DateTimeOffset? createdDate = null,
            DateTimeOffset? lastUpdatedTime = null)
        {
            var warehouse = (WarehouseModels.Warehouse)RuntimeHelpers.GetUninitializedObject(typeof(WarehouseModels.Warehouse));
            SetField(warehouse, "Id", (Guid?)id);
            SetField(warehouse, "DisplayName", displayName);
            SetField(warehouse, "Description", description);

            var props = (WarehouseModels.WarehouseProperties)RuntimeHelpers.GetUninitializedObject(typeof(WarehouseModels.WarehouseProperties));
            SetField(props, "ConnectionString", connectionString);
            SetField(props, "CreatedDate", createdDate);
            SetField(props, "LastUpdatedTime", lastUpdatedTime);
            SetField(warehouse, "Properties", props);

            return warehouse;
        }

        public static WarehouseModels.ConnectionStringResponse CreateConnectionStringResult(string connectionString)
        {
            var connStr = (WarehouseModels.ConnectionStringResponse)RuntimeHelpers.GetUninitializedObject(typeof(WarehouseModels.ConnectionStringResponse));
            SetField(connStr, "ConnectionString", connectionString);
            return connStr;
        }

        public static LakehouseModels.Lakehouse CreateLakehouse(Guid id, string displayName)
        {
            var lakehouse = (LakehouseModels.Lakehouse)RuntimeHelpers.GetUninitializedObject(typeof(LakehouseModels.Lakehouse));
            SetField(lakehouse, "Id", (Guid?)id);
            SetField(lakehouse, "DisplayName", displayName);
            return lakehouse;
        }

        public static MirroredDbModels.MirroredDatabase CreateMirroredDatabase(Guid id, string displayName)
        {
            var db = (MirroredDbModels.MirroredDatabase)RuntimeHelpers.GetUninitializedObject(typeof(MirroredDbModels.MirroredDatabase));
            SetField(db, "Id", (Guid?)id);
            SetField(db, "DisplayName", displayName);
            return db;
        }

        public static MirroredDbModels.MirroringStatusResponse CreateMirroringStatusResponse(object? status)
        {
            var resp = (MirroredDbModels.MirroringStatusResponse)RuntimeHelpers.GetUninitializedObject(typeof(MirroredDbModels.MirroringStatusResponse));
            SetField(resp, "Status", status);
            return resp;
        }

        public static MirroredDbModels.MirroredDatabaseDefinitionResponse CreateMirroredDatabaseDefinitionResponse(string path, string payload)
        {
            var resp = (MirroredDbModels.MirroredDatabaseDefinitionResponse)RuntimeHelpers.GetUninitializedObject(typeof(MirroredDbModels.MirroredDatabaseDefinitionResponse));
            var def = (MirroredDbModels.MirroredDatabaseDefinition)RuntimeHelpers.GetUninitializedObject(typeof(MirroredDbModels.MirroredDatabaseDefinition));
            var part = (MirroredDbModels.MirroredDatabaseDefinitionPart)RuntimeHelpers.GetUninitializedObject(typeof(MirroredDbModels.MirroredDatabaseDefinitionPart));
            SetField(part, "Path", path);
            SetField(part, "Payload", payload);
            SetField(def, "Parts", new List<MirroredDbModels.MirroredDatabaseDefinitionPart> { part });
            SetField(resp, "Definition", def);
            return resp;
        }

        public static AsyncPageable<T> CreateAsyncPageable<T>(IEnumerable<T> items) where T : notnull
        {
            var page = Page<T>.FromValues(items.ToList(), null, Mock.Of<Response>());
            return AsyncPageable<T>.FromPages(new[] { page });
        }

        public static (
            FabricClient Client,
            Mock<WorkspacesClient> Workspaces,
            Mock<Microsoft.Fabric.Api.Warehouse.ItemsClient> WarehouseItems,
            Mock<Microsoft.Fabric.Api.Lakehouse.ItemsClient> LakehouseItems,
            Mock<TablesClient> LakehouseTables,
            Mock<Microsoft.Fabric.Api.MirroredDatabase.ItemsClient> MirroredDbItems,
            Mock<MirroringClient> MirroredDbMirroring
        ) CreateMockFabricClientHierarchy()
        {
            var fabricClient = (FabricClient)RuntimeHelpers.GetUninitializedObject(typeof(FabricClient));

            // Core client
            var coreClient = (FabricCoreClient)RuntimeHelpers.GetUninitializedObject(typeof(FabricCoreClient));
            var mockWorkspaces = new Mock<WorkspacesClient>();
            SetField(fabricClient, "_coreClient", coreClient);
            SetField(coreClient, "_workspacesClient", mockWorkspaces.Object);

            // Warehouse client
            var warehouseClient = (FabricWarehouseClient)RuntimeHelpers.GetUninitializedObject(typeof(FabricWarehouseClient));
            var mockWarehouseItems = new Mock<Microsoft.Fabric.Api.Warehouse.ItemsClient>();
            SetField(fabricClient, "_warehouseClient", warehouseClient);
            SetField(warehouseClient, "_itemsClient", mockWarehouseItems.Object);

            // Lakehouse client
            var lakehouseClient = (FabricLakehouseClient)RuntimeHelpers.GetUninitializedObject(typeof(FabricLakehouseClient));
            var mockLakehouseItems = new Mock<Microsoft.Fabric.Api.Lakehouse.ItemsClient>();
            var mockLakehouseTables = new Mock<TablesClient>();
            SetField(fabricClient, "_lakehouseClient", lakehouseClient);
            SetField(lakehouseClient, "_itemsClient", mockLakehouseItems.Object);
            SetField(lakehouseClient, "_tablesClient", mockLakehouseTables.Object);

            // MirroredDatabase client
            var mirroredDbClient = (FabricMirroredDatabaseClient)RuntimeHelpers.GetUninitializedObject(typeof(FabricMirroredDatabaseClient));
            var mockMirroredDbItems = new Mock<Microsoft.Fabric.Api.MirroredDatabase.ItemsClient>();
            var mockMirroredDbMirroring = new Mock<MirroringClient>();
            SetField(fabricClient, "_mirroredDatabaseClient", mirroredDbClient);
            SetField(mirroredDbClient, "_itemsClient", mockMirroredDbItems.Object);
            SetField(mirroredDbClient, "_mirroringClient", mockMirroredDbMirroring.Object);

            return (
                fabricClient,
                mockWorkspaces,
                mockWarehouseItems,
                mockLakehouseItems,
                mockLakehouseTables,
                mockMirroredDbItems,
                mockMirroredDbMirroring
            );
        }
    }
}
