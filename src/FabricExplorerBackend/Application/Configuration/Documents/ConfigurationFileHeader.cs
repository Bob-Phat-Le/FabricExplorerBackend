using System.Text.Json;

namespace FabricExplorerBackend.Application.Configuration.Documents
{
    /// <summary>What can be told about an uploaded file before it is turned into a <see cref="ConfigurationDocument"/>.</summary>
    public sealed record ConfigurationFileHeader(JsonValueKind Kind, long RawLength, string? Format, int? FormatVersion)
    {
        public static ConfigurationFileHeader From(JsonElement element)
        {
            // Nothing else can be read from a value that is not an object (and an undefined value has no raw text).
            if (element.ValueKind != JsonValueKind.Object)
                return new ConfigurationFileHeader(element.ValueKind, 0, null, null);

            var format = element.TryGetProperty("format", out var formatElement) && formatElement.ValueKind == JsonValueKind.String
                ? formatElement.GetString()
                : null;

            int? formatVersion = element.TryGetProperty("formatVersion", out var versionElement)
                && versionElement.ValueKind == JsonValueKind.Number
                && versionElement.TryGetInt32(out var version)
                    ? version
                    : null;

            return new ConfigurationFileHeader(element.ValueKind, element.GetRawText().Length, format, formatVersion);
        }
    }
}
