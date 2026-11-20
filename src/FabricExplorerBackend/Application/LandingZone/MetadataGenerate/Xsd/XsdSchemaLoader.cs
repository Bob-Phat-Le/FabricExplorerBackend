using System.Xml;
using System.Xml.Schema;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Infrastructures.Options;
using Microsoft.Extensions.Options;

namespace FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Xsd
{
    public sealed class XsdSchemaLoader(IOptions<LandingZoneOptions> options)
    {
        public Result<XmlSchemaSet> Load(Stream xsd)
        {
            var maxBytes = options.Value.MaxXsdBytes;
            if (xsd.Length > maxBytes)
                return Error.BadRequest(ErrorCodes.MetadataGenerate.InvalidXsd, $"The XSD file is larger than {maxBytes / 1024} KB.");

            var problems = new List<string>();
            ValidationEventHandler collect = (_, e) =>
            {
                if (e.Severity == XmlSeverityType.Error) problems.Add(e.Message);
            };

            var schemas = new XmlSchemaSet { XmlResolver = null };
            schemas.ValidationEventHandler += collect;

            try
            {
                using var reader = XmlReader.Create(xsd, new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    MaxCharactersInDocument = maxBytes
                });

                schemas.Add(XmlSchema.Read(reader, collect)!);
                schemas.Compile();
            }
            catch (Exception ex) when (ex is XmlException or XmlSchemaException)
            {
                problems.Add(ex.Message);
            }

            return problems.Count == 0
                ? Result<XmlSchemaSet>.Success(schemas)
                : Error.BadRequest(ErrorCodes.MetadataGenerate.InvalidXsd, $"The file is not a valid XSD. {string.Join("; ", problems)}");
        }
    }
}
