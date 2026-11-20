using FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate;
using FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Formats;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Generation
{
    public sealed class MetadataGenerator(IMetadataFormatRegistry formats) : IMetadataGenerator
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public PreviewResponse Generate(PreviewRequest request)
        {
            if (!formats.TryGet(request.FileFormat, out var format))
                return Invalid($"Unsupported file format '{request.FileFormat}'. Use {string.Join(", ", formats.All.Select(f => f.Name))}.");

            var errors = new List<string>();

            if (request.KeyColumns.Any(string.IsNullOrWhiteSpace))
                errors.Add("Key column name cannot be empty.");

            errors.AddRange(request.KeyColumns
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .GroupBy(k => k.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => $"Duplicate key column: {g.Key}."));

            format.Validate(request, errors);
            if (errors.Count > 0) return Invalid(errors.ToArray());

            var metadata = new Dictionary<string, object?>();
            if (request.KeyColumns.Count > 0)
                metadata["keyColumns"] = request.KeyColumns.Select(k => k.Trim()).ToList();

            foreach (var (key, value) in format.Build(request))
                metadata[key] = value;

            return new PreviewResponse(OpenMirroringConventions.MetadataFileName, JsonSerializer.Serialize(metadata, JsonOptions), true, []);
        }

        private static PreviewResponse Invalid(params string[] errors) =>
            new(OpenMirroringConventions.MetadataFileName, string.Empty, false, errors);
    }
}
