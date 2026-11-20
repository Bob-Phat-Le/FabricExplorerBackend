using FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate;

namespace FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Formats
{
    /// <summary>Strategy for one Landing Zone file format: how it is validated and what it writes into _metadata.json.</summary>
    public interface IMetadataFormat
    {
        string Name { get; }
        string Title { get; }
        string Description { get; }
        bool RequiresColumnTypes { get; }
        IReadOnlyList<FormatOption> Options { get; }

        void Validate(PreviewRequest request, ICollection<string> errors);

        /// <summary>The format specific properties, in the order they are written.</summary>
        IEnumerable<KeyValuePair<string, object?>> Build(PreviewRequest request);
    }

    public interface IMetadataFormatRegistry
    {
        IReadOnlyList<IMetadataFormat> All { get; }
        bool TryGet(string? name, out IMetadataFormat format);
        MetadataCapabilities Describe();
    }
}
