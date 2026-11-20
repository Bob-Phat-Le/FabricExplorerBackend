using System.Xml.Schema;

namespace FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Xsd
{
    public sealed record DiscoveredRecord(string Name, XmlSchemaComplexType Type);

    public interface IRecordDiscoveryStrategy
    {
        IEnumerable<DiscoveredRecord> Discover(XmlSchemaElement root, XmlSchemaComplexType rootType);
    }

    public sealed class RepeatingChildRecords : IRecordDiscoveryStrategy
    {
        public IEnumerable<DiscoveredRecord> Discover(XmlSchemaElement root, XmlSchemaComplexType rootType) =>
            rootType.ContentTypeParticle.Elements()
                .Select(x => x.Element)
                .Where(child => child.IsRepeating() && child.ElementSchemaType is XmlSchemaComplexType nested && nested.IsNested())
                .Select(child => new DiscoveredRecord(child.QualifiedName.Name, (XmlSchemaComplexType)child.ElementSchemaType!));
    }

    public sealed class GlobalElementRecord : IRecordDiscoveryStrategy
    {
        public IEnumerable<DiscoveredRecord> Discover(XmlSchemaElement root, XmlSchemaComplexType rootType)
        {
            yield return new DiscoveredRecord(root.QualifiedName.Name, rootType);
        }
    }
}
