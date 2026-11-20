using FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate;
using FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Typing;
using FabricExplorerBackend.Infrastructures.Options;
using Microsoft.Extensions.Options;
using System.Xml.Schema;

namespace FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Xsd
{
    public sealed class XsdFieldExtractor(IFabricTypeResolver typeResolver, IOptions<LandingZoneOptions> options)
    {
        public IReadOnlyList<XsdRecordField> Extract(XmlSchemaComplexType recordType)
        {
            var fields = new List<XsdRecordField>();

            foreach (var (element, optional) in recordType.ContentTypeParticle.Elements())
            {
                fields.Add(Describe(
                    element.QualifiedName.Name,
                    element.ElementSchemaType,
                    nullable: optional || element.MinOccurs == 0 || element.IsNillable,
                    repeating: element.IsRepeating(),
                    attribute: false));
            }

            foreach (var attribute in recordType.AttributeUses.Values.OfType<XmlSchemaAttribute>().Where(a => a.Use != XmlSchemaUse.Prohibited))
            {
                fields.Add(Describe(
                    attribute.QualifiedName.Name,
                    attribute.AttributeSchemaType,
                    nullable: attribute.Use != XmlSchemaUse.Required,
                    repeating: false,
                    attribute: true));
            }

            return fields.Where(f => f.Name.Length > 0).ToList();
        }

        private XsdRecordField Describe(string name, XmlSchemaType? type, bool nullable, bool repeating, bool attribute)
        {
            var nested = type.IsNested();
            var scalar = !nested && !repeating;
            var fabricType = scalar ? typeResolver.Resolve(type) : string.Empty;

            var xsdType = (nested ? "complexType" : type.DisplayName()) + (repeating ? "[]" : string.Empty) + (attribute ? " (@attribute)" : string.Empty);
            var keyCandidate = scalar && !nullable && fabricType.Length > 0
                && options.Value.KeyColumnNameSuffixes.Any(suffix => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

            return new XsdRecordField(name, xsdType, fabricType, nullable, keyCandidate, attribute, nested, repeating);
        }
    }
}
