using System.Text.Json.Serialization;

namespace FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum FormatOptionKind
    {
        Text,
        EscapedText,
        Choice,
        Flag
    }

    public record FormatOption(
        string Key,
        string Label,
        FormatOptionKind Kind = FormatOptionKind.Text,
        string DefaultValue = "",
        bool Required = false,
        bool ReadOnly = false,
        string? Hint = null,
        IReadOnlyList<string>? Choices = null,
        string? Pattern = null);
}
