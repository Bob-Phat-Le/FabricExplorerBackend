using FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate;
using FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Typing;

namespace FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Formats
{
    public sealed class MetadataFormatRegistry : IMetadataFormatRegistry
    {
        private readonly Dictionary<string, IMetadataFormat> byName;

        public MetadataFormatRegistry(IEnumerable<IMetadataFormat> formats)
        {
            All = formats.ToList();
            byName = All.ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyList<IMetadataFormat> All { get; }

        public bool TryGet(string? name, out IMetadataFormat format)
        {
            format = null!;
            return name is not null && byName.TryGetValue(name.Trim(), out format!);
        }

        public MetadataCapabilities Describe() => new(
            All.Select(f => new MetadataFormatInfo(f.Name, f.Title, f.Description, f.RequiresColumnTypes, f.Options)).ToList(),
            FabricDataTypes.All,
            OpenMirroringConventions.MetadataFileName,
            OpenMirroringConventions.SchemaFolderSuffix);
    }
}
