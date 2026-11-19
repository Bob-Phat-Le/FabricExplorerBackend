using Azure.Core;
using FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Infrastructures.LandingZone;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Schema;

namespace FabricExplorerBackend.Application.LandingZone
{
    public partial class LandingZoneService
    {
        public async Task<Result<AnalyzeResponse>> AnalyzeXsdAsync(AnalyzeRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var errors = new List<string>();

            using var stream = request.File.OpenReadStream();
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            });

            var schema = XmlSchema.Read(reader, (_, e) => errors.Add(e.Message));

            if (schema is null || errors.Count > 0)
                throw new InvalidOperationException(string.Join("; ", errors));

            var schemas = new XmlSchemaSet { XmlResolver = null };
            schemas.ValidationEventHandler += (_, e) => errors.Add(e.Message);
            schemas.Add(schema);
            schemas.Compile();

            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join("; ", errors));

            var records = new List<XsdRecord>();

            foreach (XmlSchemaElement root in schemas.GlobalElements.Values)
            {
                if (root.ElementSchemaType is not XmlSchemaComplexType rootType ||
                    rootType.Particle is not XmlSchemaGroupBase rootGroup)
                    continue;

                foreach (XmlSchemaObject item in rootGroup.Items)
                {
                    if (item is not XmlSchemaElement element ||
                        element.ElementSchemaType is not XmlSchemaComplexType recordType ||
                        (element.MaxOccurs <= 1 && element.MaxOccursString != "unbounded") ||
                        recordType.Particle is not XmlSchemaGroupBase recordGroup)
                        continue;

                    var fields = new List<XsdRecordField>();

                    foreach (XmlSchemaObject fieldItem in recordGroup.Items)
                    {
                        if (fieldItem is not XmlSchemaElement field ||
                            field.ElementSchemaType is XmlSchemaComplexType)
                            continue;

                        var xsdType = field.ElementSchemaType?.Datatype?.TypeCode.ToString() ?? "Unknown";
                        var fieldName = field.Name ?? field.RefName.Name;

                        var fabricType = xsdType switch
                        {
                            "String" => "string",
                            "Boolean" => "boolean",
                            "Int" => "int32",
                            "Long" => "int64",
                            "Short" => "int16",
                            "Decimal" => "decimal",
                            "Float" => "float",
                            "Double" => "double",
                            "Date" => "date",
                            "DateTime" => "timestamp",
                            _ => "string"
                        };

                        fields.Add(new XsdRecordField(
                            Name: fieldName,
                            XsdType: xsdType,
                            RecommendedFabricType: fabricType,
                            Nullable: field.MinOccurs == 0 || field.IsNillable,
                            IsKeyCandidate: fieldName.Equals("id", StringComparison.OrdinalIgnoreCase)
                        ));
                    }

                    records.Add(new XsdRecord(
                        Name: element.Name ?? element.RefName.Name,
                        Fields: fields
                    ));
                }
            }

            return
                Result<AnalyzeResponse>.Success(
                    new AnalyzeResponse(
                        TargetNamespace: schema.TargetNamespace ?? string.Empty,
                        Records: records
                    )
                );
        }

        public async Task<Result<PreviewResponse>> Preview(PreviewRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var errors = new List<string>();

            var isCsv = request.FileFormat == "CSV";
            var isDelimitedText = request.FileFormat == "DelimitedText";
            var isParquet = request.FileFormat == "Parquet";

            if (!isCsv && !isDelimitedText && !isParquet)
                errors.Add("Unsupported file format.");

            var includedColumns = request.Columns
                .Where(c => c.Included)
                .ToList();

            if (isCsv || isDelimitedText)
            {
                if (includedColumns.Count == 0)
                    errors.Add("At least one column must be included.");

                var duplicatedNames = includedColumns
                    .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                    .GroupBy(c => c.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);

                if (duplicatedNames.Any())
                    errors.Add($"Duplicate column names: {string.Join(", ", duplicatedNames)}.");

                if (includedColumns.Any(c => string.IsNullOrWhiteSpace(c.Name)))
                    errors.Add("Column name cannot be empty.");

                if (includedColumns.Any(c => string.IsNullOrWhiteSpace(c.FabricType)))
                    errors.Add("Every included column must have a Fabric data type.");

                if (request.KeyColumns.Any(key =>
                    !includedColumns.Any(c => c.Name == key)))
                    errors.Add("Every key column must exist in the included columns.");
            }

            if (isDelimitedText &&
                string.IsNullOrWhiteSpace(request.Options?.FileExtension))
                errors.Add("FileExtension is required for DelimitedText.");

            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join("; ", errors));

            var metadata = new Dictionary<string, object?>();

            if (request.KeyColumns.Count > 0)
                metadata["keyColumns"] = request.KeyColumns;

            if (isCsv || isDelimitedText)
            {
                var options = request.Options!;

                metadata["FileFormat"] = request.FileFormat;
                metadata["FirstRowAsHeader"] = options.FirstRowAsHeader;
                metadata["ColumnSeparator"] = options.ColumnSeparator;
                metadata["RowSeparator"] = options.RowSeparator;
                metadata["QuoteCharacter"] = options.QuoteCharacter;
                metadata["EscapeCharacter"] = options.EscapeCharacter;
                metadata["Encoding"] = options.Encoding;

                if (!string.IsNullOrEmpty(options.NullValue))
                    metadata["NullValue"] = options.NullValue;

                if (isDelimitedText)
                    metadata["FileExtension"] = options.FileExtension!.Trim();

                metadata["SchemaDefinition"] = new
                {
                    Columns = includedColumns.Select(c => new
                    {
                        Name = c.Name.Trim(),
                        DataType = c.FabricType,
                        IsNullable = c.Nullable
                    })
                };
            }
            else
            {
                metadata["FileFormat"] = "Parquet";
            }

            return Result<PreviewResponse>.Success(new PreviewResponse(
                FileName: "_metadata.json",
                Json: JsonSerializer.Serialize(metadata, new JsonSerializerOptions
                {
                    WriteIndented = true
                }))
            );
        }

        public async Task<Result<PublishResponse>> PublishAsync(Guid connectionId, PublishRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Validate folder names.
            if (string.IsNullOrWhiteSpace(request.SchemaName) ||
                string.IsNullOrWhiteSpace(request.TableName) ||
                request.SchemaName is "." or ".." ||
                request.TableName is "." or ".." ||
                request.SchemaName.IndexOfAny(['/', '\\']) >= 0 ||
                request.TableName.IndexOfAny(['/', '\\']) >= 0)
            {
                return Error.BadRequest(ErrorCodes.LandingZone.BadRequest, "Schema name or table name is invalid.");
            }

            // Validate parent path against path traversal.
            var parentSegments = (request.ParentPath ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (parentSegments.Any(segment => segment is "." or "..") ||
                request.ParentPath?.Contains('\\') == true)
            {
                return Error.BadRequest(ErrorCodes.LandingZone.BadRequest, "Parent path is invalid.");
            }

            // Generate metadata using the same logic as Preview endpoint.
            var preview = await Preview(request.Metadata);
            var content = Encoding.UTF8.GetBytes(preview.Data!.Json);

            cancellationToken.ThrowIfCancellationRequested();

            // Open Landing Zone session.
            var sessionResult = await landingZoneHelper.OpenAsync(
                connectionId,
                cancellationToken);

            if (!sessionResult.IsSuccess)
                return sessionResult.Errors;

            var session = sessionResult.Value;
            var fileSystem = session.FileSystem;

            // Build destination paths relative to Landing Zone root.
            var schemaFolder = LandingZonePaths.Normalize(
                string.Join('/', parentSegments.Append($"{request.SchemaName}.schema")));

            var tableFolder = LandingZonePaths.Normalize(
                $"{schemaFolder}/{request.TableName}");

            var filePath = $"{tableFolder}/_metadata.json";

            // Ensure the selected parent folder exists.
            if (parentSegments.Length > 0)
            {
                var parentPath = LandingZonePaths.Normalize(string.Join('/', parentSegments));

                if (!await fileSystem.ExistsAsync(parentPath, cancellationToken))
                {
                    return Error.NotFound(
                        ErrorCodes.LandingZone.PathNotFound,
                        $"Parent folder '{parentPath}' was not found.");
                }
            }

            // Check existing metadata file.
            var exists = await fileSystem.ExistsAsync(filePath, cancellationToken);

            if (exists && !request.Overwrite)
            {
                return Error.Conflict(ErrorCodes.MetadataGenerate.MetadataAlreadyExists, "_metadata.json already exists in the target folder.");
            }

            // Create schema folder if necessary.
            if (!await fileSystem.ExistsAsync(schemaFolder, cancellationToken))
            {
                await fileSystem.CreateDirectoryAsync(
                    schemaFolder,
                    cancellationToken);
            }

            // Create table folder if necessary.
            if (!await fileSystem.ExistsAsync(tableFolder, cancellationToken))
            {
                await fileSystem.CreateDirectoryAsync(
                    tableFolder,
                    cancellationToken);
            }

            // Upload metadata JSON.
            await using var stream = new MemoryStream(content);

            await fileSystem.UploadAsync(
                filePath,
                stream,
                request.Overwrite,
                cancellationToken);

            return Result<PublishResponse>.Success(
                new PublishResponse(
                    FilePath: filePath,
                    Overwritten: exists));
        }
    }
}
