using FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate;
using FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Typing;

namespace FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Formats
{
    public abstract class DelimitedMetadataFormat : MetadataFormatBase
    {
        public override bool RequiresColumnTypes => true;

        protected static IReadOnlyList<FormatOption> OptionSet(string columnSeparator, params FormatOption[] leading) =>
        [
            ..leading,
            new("FirstRowAsHeader", "FirstRowAsHeader", FormatOptionKind.Flag, "true", Required: true, ReadOnly: true),
            new("ColumnSeparator", "ColumnSeparator", FormatOptionKind.EscapedText, columnSeparator, Required: true, Hint: "Type \\t for a tab."),
            new("RowSeparator", "RowSeparator", FormatOptionKind.EscapedText, "\\r\\n", Required: true, Hint: "Type \\r\\n or \\n."),
            new("QuoteCharacter", "QuoteCharacter", DefaultValue: "\""),
            new("EscapeCharacter", "EscapeCharacter", DefaultValue: "\\"),
            new("Encoding", "Encoding", FormatOptionKind.Choice, "UTF-8", Required: true, Choices: ["UTF-8", "UTF-16"]),
            new("NullValue", "NullValue (optional)", Hint: "For example NULL.")
        ];

        public override void Validate(PreviewRequest request, ICollection<string> errors)
        {
            base.Validate(request, errors);

            var columns = request.Columns.Where(c => c.Included).ToList();
            if (columns.Count == 0) errors.Add("At least one column must be included.");

            if (columns.Any(c => string.IsNullOrWhiteSpace(c.Name)))
                errors.Add("Column name cannot be empty.");

            var duplicated = columns
                .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                .GroupBy(c => c.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);
            foreach (var name in duplicated)
                errors.Add($"Duplicate column name: {name}.");

            foreach (var column in columns.Where(c => !string.IsNullOrWhiteSpace(c.Name) && !FabricDataTypes.IsSupported(c.FabricType)))
                errors.Add($"Column '{column.Name.Trim()}' needs a supported Fabric data type ({string.Join(", ", FabricDataTypes.All)}).");

            foreach (var key in request.KeyColumns.Where(k => !columns.Any(c => c.Name.Trim() == k.Trim())))
                errors.Add($"Key column '{key}' is excluded or missing from the schema.");
        }

        public override IEnumerable<KeyValuePair<string, object?>> Build(PreviewRequest request)
        {
            foreach (var property in base.Build(request))
                yield return property;

            yield return new("SchemaDefinition", new
            {
                Columns = request.Columns
                    .Where(c => c.Included)
                    .Select(c => new { Name = c.Name.Trim(), DataType = c.FabricType, IsNullable = c.Nullable })
                    .ToList()
            });
        }
    }

    public sealed class CsvMetadataFormat : DelimitedMetadataFormat
    {
        public override string Name => "CSV";
        public override string Title => "CSV (Comma Separated)";
        public override string Description => "Requires explicit Fabric data types in SchemaDefinition.";
        public override IReadOnlyList<FormatOption> Options { get; } = OptionSet(",");
    }

    public sealed class DelimitedTextMetadataFormat : DelimitedMetadataFormat
    {
        public override string Name => "DelimitedText";
        public override string Title => "DelimitedText (TSV / Custom)";
        public override string Description => "Custom file extension and delimiters, with explicit Fabric data types.";
        public override IReadOnlyList<FormatOption> Options { get; } = OptionSet(
            "\\t",
            new FormatOption("FileExtension", "FileExtension", DefaultValue: "tsv", Required: true,
                Hint: "Letters and digits only, without the dot.", Pattern: "^[A-Za-z0-9]{1,16}$"));
    }

    public sealed class ParquetMetadataFormat : MetadataFormatBase
    {
        public override string Name => "Parquet";
        public override string Title => "Parquet";
        public override string Description => "Self-describing: no delimiter options and no data type mapping are needed.";
    }
}
