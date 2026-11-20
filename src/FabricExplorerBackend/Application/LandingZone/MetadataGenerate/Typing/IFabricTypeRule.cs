using System.Xml.Schema;

namespace FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Typing
{
    public interface IFabricTypeRule
    {
        string? Resolve(XmlTypeCode code, XsdFacets facets);
    }

    public interface IFabricTypeResolver
    {
        string Resolve(XmlSchemaType? type);
    }
}
