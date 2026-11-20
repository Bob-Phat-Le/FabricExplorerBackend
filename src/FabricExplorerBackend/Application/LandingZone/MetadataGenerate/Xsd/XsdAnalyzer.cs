using System.Xml.Schema;
using FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.Results;

namespace FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Xsd
{
    public interface IXsdAnalyzer
    {
        Result<AnalyzeResponse> Analyze(Stream xsd);
    }

    public sealed class XsdAnalyzer(
        XsdSchemaLoader loader,
        XsdFieldExtractor fieldExtractor,
        IEnumerable<IRecordDiscoveryStrategy> discoveryStrategies) : IXsdAnalyzer
    {
        public Result<AnalyzeResponse> Analyze(Stream xsd)
        {
            var loaded = loader.Load(xsd);
            if (!loaded.IsSuccess)
                return Result<AnalyzeResponse>.Failure(loaded.Errors.ToArray());

            var schemas = loaded.Data!;
            var records = new List<XsdRecord>();

            foreach (var root in schemas.GlobalElements.Values.OfType<XmlSchemaElement>())
            {
                if (root.ElementSchemaType is not XmlSchemaComplexType rootType) continue;

                var discovered = discoveryStrategies
                    .Select(strategy => strategy.Discover(root, rootType).ToList())
                    .FirstOrDefault(found => found.Count > 0) ?? new List<DiscoveredRecord>();

                records.AddRange(discovered
                    .Where(found => records.All(r => r.Name != found.Name))
                    .Select(found => new XsdRecord(found.Name, fieldExtractor.Extract(found.Type)))
                    .Where(candidate => candidate.Fields.Count > 0)
                    .ToList());
            }

            if (records.Count == 0)
                return Error.BadRequest(ErrorCodes.MetadataGenerate.InvalidXsd, "No table-like record element with child elements or attributes was found in this XSD.");

            var userTypes = schemas.GlobalTypes.Values.OfType<XmlSchemaType>().Where(t => t.QualifiedName.Namespace != XmlSchema.Namespace).ToList();
            var targetNamespace = schemas.Schemas().OfType<XmlSchema>().Select(s => s.TargetNamespace).FirstOrDefault(ns => !string.IsNullOrEmpty(ns));

            return Result<AnalyzeResponse>.Success(new AnalyzeResponse(
                targetNamespace ?? string.Empty,
                records,
                userTypes.Count(t => t is XmlSchemaComplexType),
                userTypes.Count(t => t is XmlSchemaSimpleType)));
        }
    }
}
