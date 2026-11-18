using FabricExplorerBackend.Application.LandingZone.Contracts;
using System.Text;
using System.Text.Json;

namespace FabricExplorerBackend.Application.LandingZone
{
    /// <summary>Builds the "Partial File Preview" of the file drawer from the first bytes of a file.</summary>
    public static class LandingZonePreviewBuilder
    {
        private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            "txt", "md", "log", "xml", "xsd", "yaml", "yml", "sql", "ini", "config", "jsonl"
        };
        private static readonly char[] CsvDelimiters = [',', ';', '\t', '|'];
        private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

        public static LandingZonePreviewResponse Unsupported()
            => new(LandingZonePreviewKinds.Unsupported, null, null, null, false, null);

        public static bool IsPreviewable(string fileName)
        {
            var extension = LandingZoneContentTypes.ExtensionOf(fileName);
            return extension is not null
                   && (extension is "csv" or "tsv" or "json" || TextExtensions.Contains(extension));
        }

        public static LandingZonePreviewResponse Build(string fileName, byte[] head, long fileSize, int csvRows)
        {
            var extension = LandingZoneContentTypes.ExtensionOf(fileName);
            if (extension is null || !IsPreviewable(fileName)) return Unsupported();

            var truncated = fileSize > head.Length;
            var text = Decode(head);

            // Binary content that merely carries a text extension.
            if (text.Contains('\0')) return Unsupported();

            // A multi-byte character may have been cut at the end of the buffer.
            if (truncated) text = text.TrimEnd('\uFFFD');

            return extension switch
            {
                "csv" => BuildCsv(text, ',', truncated, csvRows, detect: true),
                "tsv" => BuildCsv(text, '\t', truncated, csvRows, detect: false),
                "json" => BuildJson(text, truncated),
                _ => new LandingZonePreviewResponse(LandingZonePreviewKinds.Text, null, null, text, truncated, null)
            };
        }

        private static LandingZonePreviewResponse BuildJson(string text, bool truncated)
        {
            if (!truncated)
            {
                try
                {
                    using var document = JsonDocument.Parse(text);
                    text = JsonSerializer.Serialize(document.RootElement, Indented);
                }
                catch (JsonException)
                {
                    // Not valid JSON: show it as it is.
                }
            }

            return new LandingZonePreviewResponse(LandingZonePreviewKinds.Json, null, null, text, truncated, null);
        }

        private static LandingZonePreviewResponse BuildCsv(string text, char delimiter, bool truncated, int dataRows, bool detect)
        {
            if (detect) delimiter = DetectDelimiter(text, delimiter);

            var records = ParseCsv(text, delimiter, dataRows + 1, truncated, out var hasMore);
            if (records.Count == 0)
                return new LandingZonePreviewResponse(LandingZonePreviewKinds.Csv, [], [], null, truncated, delimiter.ToString());

            var headers = records[0];
            var rows = records.Skip(1).Select(r => (IReadOnlyList<string>)r).ToList();

            return new LandingZonePreviewResponse(
                LandingZonePreviewKinds.Csv, headers, rows, null, truncated || hasMore, delimiter.ToString());
        }

        private static char DetectDelimiter(string text, char fallback)
        {
            var newline = text.IndexOfAny(['\r', '\n']);
            var firstLine = newline < 0 ? text : text[..newline];

            var best = fallback;
            var bestCount = 0;
            foreach (var candidate in CsvDelimiters)
            {
                var count = firstLine.Count(c => c == candidate);
                if (count > bestCount)
                {
                    best = candidate;
                    bestCount = count;
                }
            }

            return best;
        }

        /// <summary>
        /// Minimal RFC 4180 reader: quoted fields, doubled quotes and line breaks inside quotes.
        /// Stops after <paramref name="maxRecords"/>. A last record cut by the end of a truncated buffer is dropped.
        /// </summary>
        internal static List<List<string>> ParseCsv(string text, char delimiter, int maxRecords, bool truncated, out bool hasMore)
        {
            var records = new List<List<string>>();
            var record = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;
            var atFieldStart = true;
            var rowHasContent = false;
            hasMore = false;

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(c);
                    }

                    continue;
                }

                if (c == '"' && atFieldStart)
                {
                    inQuotes = true;
                    atFieldStart = false;
                    rowHasContent = true;
                }
                else if (c == delimiter)
                {
                    record.Add(field.ToString());
                    field.Clear();
                    atFieldStart = true;
                    rowHasContent = true;
                }
                else if (c is '\r' or '\n')
                {
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;

                    // Blank lines are skipped.
                    if (rowHasContent)
                    {
                        record.Add(field.ToString());
                        field.Clear();
                        records.Add(record);
                        record = new List<string>();
                        rowHasContent = false;

                        if (records.Count >= maxRecords)
                        {
                            hasMore = i + 1 < text.Length;
                            return records;
                        }
                    }

                    atFieldStart = true;
                }
                else
                {
                    field.Append(c);
                    atFieldStart = false;
                    rowHasContent = true;
                }
            }

            // End of text: the last record is complete only if the whole file was read.
            if (!truncated && !inQuotes && rowHasContent)
            {
                record.Add(field.ToString());
                records.Add(record);
            }
            else if (truncated)
            {
                hasMore = true;
            }

            return records;
        }

        private static string Decode(byte[] bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

            return Encoding.UTF8.GetString(bytes);
        }
    }
}
