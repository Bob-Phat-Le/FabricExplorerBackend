using System.Text;
using System.Text.Json;

namespace FabricExplorerBackend.Features.Fabric.MirroredDatabase
{
    // Thông tin lấy từ phần mirroring.json trong definition của mirrored database
    public record MirroringDefinitionInfo(string? SourceType, string? SourceName, string? DefaultSchema)
    {
        public static MirroringDefinitionInfo? Parse(string? base64Payload)
        {
            if (string.IsNullOrWhiteSpace(base64Payload))
                return null;

            try
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64Payload));
                using var document = JsonDocument.Parse(json);

                if (!document.RootElement.TryGetProperty("properties", out var properties))
                    return null;

                string? sourceType = null, sourceName = null, defaultSchema = null;

                if (properties.TryGetProperty("source", out var source))
                {
                    sourceType = GetString(source, "type");
                    if (source.TryGetProperty("typeProperties", out var sourceProps))
                        sourceName = GetString(sourceProps, "database") ?? GetString(sourceProps, "dbName");
                }

                if (properties.TryGetProperty("target", out var target)
                    && target.TryGetProperty("typeProperties", out var targetProps))
                    defaultSchema = GetString(targetProps, "defaultSchema");

                return new MirroringDefinitionInfo(sourceType, sourceName ?? sourceType, defaultSchema);
            }
            catch (Exception ex) when (ex is FormatException or JsonException)
            {
                return null;
            }
        }

        private static string? GetString(JsonElement element, string name)
            => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
    }
}
