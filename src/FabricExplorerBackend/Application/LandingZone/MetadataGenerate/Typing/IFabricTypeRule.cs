using System.Xml.Schema;

namespace FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Typing
{
    /// <summary>One link of the type-mapping chain: returns null when the rule does not apply, so the next rule decides.</summary>
    public interface IFabricTypeRule
    {
        string? Resolve(XmlTypeCode code, XsdFacets facets);
    }

    public interface IFabricTypeResolver
    {
        /// <summary>Recommended Fabric type for an XSD simple type, or empty when the user has to choose.</summary>
        string Resolve(XmlSchemaType? type);
    }
}
