using System.Globalization;
using System.Xml.Schema;

namespace FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Typing
{
    /// <summary>Numeric restriction facets of an XSD simple type (the facet nearest to the type wins).</summary>
    public sealed record XsdFacets(decimal? Min, decimal? Max, int? TotalDigits, int? FractionDigits)
    {
        public static XsdFacets From(XmlSchemaType? type)
        {
            decimal? min = null, max = null;
            int? totalDigits = null, fractionDigits = null;

            static decimal? Decimal(string? text) =>
                decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value) ? value : null;

            static int? Integer(string? text) =>
                int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

            for (var current = type as XmlSchemaSimpleType; current is not null; current = current.BaseXmlSchemaType as XmlSchemaSimpleType)
            {
                if (current.Content is not XmlSchemaSimpleTypeRestriction restriction) continue;

                foreach (var facet in restriction.Facets.OfType<XmlSchemaFacet>())
                {
                    switch (facet)
                    {
                        case XmlSchemaMinInclusiveFacet: min ??= Decimal(facet.Value); break;
                        case XmlSchemaMinExclusiveFacet: min ??= Decimal(facet.Value) + 1; break;
                        case XmlSchemaMaxInclusiveFacet: max ??= Decimal(facet.Value); break;
                        case XmlSchemaMaxExclusiveFacet: max ??= Decimal(facet.Value) - 1; break;
                        case XmlSchemaTotalDigitsFacet: totalDigits ??= Integer(facet.Value); break;
                        case XmlSchemaFractionDigitsFacet: fractionDigits ??= Integer(facet.Value); break;
                    }
                }
            }

            return new XsdFacets(min, max, totalDigits, fractionDigits);
        }
    }
}
