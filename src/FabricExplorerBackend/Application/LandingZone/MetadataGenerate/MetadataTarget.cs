using FabricExplorerBackend.Infrastructures.LandingZone;

namespace FabricExplorerBackend.Application.LandingZone.MetadataGenerate
{
    /// <summary>Where a table's _metadata.json lives: {parent}/{schema}.schema/{table}/_metadata.json.</summary>
    public readonly record struct MetadataTarget(string ParentFolder, string SchemaFolder, string TableFolder, string FilePath)
    {
        public static MetadataTarget Resolve(string? parentPath, string? schemaName, string? tableName)
        {
            var schema = LandingZonePaths.ValidateName(schemaName);
            if (schema.EndsWith(OpenMirroringConventions.SchemaFolderSuffix, StringComparison.OrdinalIgnoreCase))
                schema = LandingZonePaths.ValidateName(schema[..^OpenMirroringConventions.SchemaFolderSuffix.Length]);

            var parent = LandingZonePaths.Normalize(parentPath);
            var schemaFolder = LandingZonePaths.Combine(parent, schema + OpenMirroringConventions.SchemaFolderSuffix);
            var tableFolder = LandingZonePaths.Combine(schemaFolder, LandingZonePaths.ValidateName(tableName));
            var filePath = LandingZonePaths.Normalize(LandingZonePaths.Combine(tableFolder, OpenMirroringConventions.MetadataFileName));

            return new MetadataTarget(parent, schemaFolder, tableFolder, filePath);
        }
    }
}
