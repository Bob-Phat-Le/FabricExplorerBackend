using System.Xml.Schema;

namespace FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Xsd
{
    internal static class XsdSchemaExtensions
    {
        /// <summary>Element particles of a compiled content model. Members of a choice (or an optional group) are optional.</summary>
        public static IEnumerable<(XmlSchemaElement Element, bool Optional)> Elements(this XmlSchemaParticle? particle, bool optional = false)
        {
            switch (particle)
            {
                case XmlSchemaElement element:
                    yield return (element, optional);
                    break;

                case XmlSchemaGroupRef groupRef:
                    foreach (var item in groupRef.Particle.Elements(optional)) yield return item;
                    break;

                case XmlSchemaGroupBase groupBase:
                    var optionalMembers = optional || groupBase is XmlSchemaChoice || groupBase.MinOccurs == 0;
                    foreach (var child in groupBase.Items.OfType<XmlSchemaParticle>())
                        foreach (var item in child.Elements(optionalMembers)) yield return item;
                    break;
            }
        }

        /// <summary>True for complex types that have child elements or attributes only (not simple content with attributes).</summary>
        public static bool IsNested(this XmlSchemaType? type) =>
            type is XmlSchemaComplexType { ContentType: not XmlSchemaContentType.TextOnly };

        public static bool IsRepeating(this XmlSchemaElement element) => element.MaxOccurs > 1;

        /// <summary>"xs:int" for built-in types, the declared name for user types.</summary>
        public static string DisplayName(this XmlSchemaType? type)
        {
            if (type?.QualifiedName is { IsEmpty: false } name)
                return name.Namespace == XmlSchema.Namespace ? $"xs:{name.Name}" : name.Name;

            var code = type?.Datatype?.TypeCode.ToString();
            return string.IsNullOrEmpty(code) || code == nameof(XmlTypeCode.None) ? "unspecified" : $"xs:{char.ToLowerInvariant(code[0])}{code[1..]}";
        }
    }
}
