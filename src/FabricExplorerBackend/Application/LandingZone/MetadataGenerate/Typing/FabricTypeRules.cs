using System.Xml.Schema;

namespace FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Typing
{
    /// <summary>Integer family with min/max facets: the smallest integer type that holds the range.</summary>
    public sealed class BoundedIntegerRule : IFabricTypeRule
    {
        private static readonly HashSet<XmlTypeCode> IntegerFamily =
        [
            XmlTypeCode.Integer, XmlTypeCode.PositiveInteger, XmlTypeCode.NonNegativeInteger,
            XmlTypeCode.NegativeInteger, XmlTypeCode.NonPositiveInteger
        ];

        private static readonly (decimal Min, decimal Max, string Type)[] Ranges =
        [
            (short.MinValue, short.MaxValue, FabricDataTypes.Int16),
            (int.MinValue, int.MaxValue, FabricDataTypes.Int32)
        ];

        public string? Resolve(XmlTypeCode code, XsdFacets facets) =>
            IntegerFamily.Contains(code) && facets is { Min: { } min, Max: { } max }
                ? Ranges.FirstOrDefault(r => min >= r.Min && max <= r.Max).Type
                : null;
    }

    /// <summary>decimal with fractionDigits = 0: an integer type chosen by totalDigits.</summary>
    public sealed class WholeNumberDecimalRule : IFabricTypeRule
    {
        private static readonly (int MaxDigits, string Type)[] Ranges =
        [
            (9, FabricDataTypes.Int32),
            (18, FabricDataTypes.Int64)
        ];

        public string? Resolve(XmlTypeCode code, XsdFacets facets) =>
            code == XmlTypeCode.Decimal && facets is { FractionDigits: 0, TotalDigits: { } digits }
                ? Ranges.FirstOrDefault(r => digits <= r.MaxDigits).Type
                : null;
    }

    /// <summary>One-to-one mapping of the XSD built-in types. Always the last rule of the chain.</summary>
    public sealed class BuiltInTypeRule : IFabricTypeRule
    {
        private static readonly IReadOnlyDictionary<XmlTypeCode, string> Map = new Dictionary<XmlTypeCode, string>
        {
            [XmlTypeCode.String] = FabricDataTypes.String,
            [XmlTypeCode.NormalizedString] = FabricDataTypes.String,
            [XmlTypeCode.Token] = FabricDataTypes.String,
            [XmlTypeCode.Language] = FabricDataTypes.String,
            [XmlTypeCode.NmToken] = FabricDataTypes.String,
            [XmlTypeCode.Name] = FabricDataTypes.String,
            [XmlTypeCode.NCName] = FabricDataTypes.String,
            [XmlTypeCode.Id] = FabricDataTypes.String,
            [XmlTypeCode.Idref] = FabricDataTypes.String,
            [XmlTypeCode.AnyUri] = FabricDataTypes.String,
            [XmlTypeCode.QName] = FabricDataTypes.String,

            [XmlTypeCode.Byte] = FabricDataTypes.Int16,
            [XmlTypeCode.UnsignedByte] = FabricDataTypes.Int16,
            [XmlTypeCode.Short] = FabricDataTypes.Int16,
            [XmlTypeCode.Int] = FabricDataTypes.Int32,
            [XmlTypeCode.UnsignedShort] = FabricDataTypes.Int32,
            [XmlTypeCode.Long] = FabricDataTypes.Int64,
            [XmlTypeCode.UnsignedInt] = FabricDataTypes.Int64,
            [XmlTypeCode.UnsignedLong] = FabricDataTypes.Int64,
            [XmlTypeCode.Integer] = FabricDataTypes.Int64,
            [XmlTypeCode.PositiveInteger] = FabricDataTypes.Int64,
            [XmlTypeCode.NonNegativeInteger] = FabricDataTypes.Int64,
            [XmlTypeCode.NegativeInteger] = FabricDataTypes.Int64,
            [XmlTypeCode.NonPositiveInteger] = FabricDataTypes.Int64,

            [XmlTypeCode.Float] = FabricDataTypes.Single,
            [XmlTypeCode.Double] = FabricDataTypes.Double,
            [XmlTypeCode.Decimal] = FabricDataTypes.Double,

            [XmlTypeCode.Boolean] = FabricDataTypes.Boolean,
            [XmlTypeCode.DateTime] = FabricDataTypes.DateTime,
            [XmlTypeCode.Date] = FabricDataTypes.Date,
            [XmlTypeCode.Time] = FabricDataTypes.Time,
            [XmlTypeCode.Base64Binary] = FabricDataTypes.ByteArray,
            [XmlTypeCode.HexBinary] = FabricDataTypes.ByteArray
        };

        public string? Resolve(XmlTypeCode code, XsdFacets facets) => Map.GetValueOrDefault(code);
    }

    /// <summary>Chain of responsibility over the registered rules (registration order = priority).</summary>
    public sealed class FabricTypeResolver(IEnumerable<IFabricTypeRule> rules) : IFabricTypeResolver
    {
        public string Resolve(XmlSchemaType? type)
        {
            var code = type?.Datatype?.TypeCode ?? XmlTypeCode.None;
            var facets = XsdFacets.From(type);

            return rules.Select(rule => rule.Resolve(code, facets)).FirstOrDefault(result => result is not null) ?? string.Empty;
        }
    }
}
