using Microsoft.AspNetCore.StaticFiles;

namespace FabricExplorerBackend.Application.LandingZone
{
    public static class LandingZoneContentTypes
    {
        public const string Default = "application/octet-stream";

        private static readonly FileExtensionContentTypeProvider Provider = Create();

        private static FileExtensionContentTypeProvider Create()
        {
            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".parquet"] = "application/vnd.apache.parquet";
            provider.Mappings[".xsd"] = "application/xml";
            provider.Mappings[".jsonl"] = "application/x-ndjson";
            provider.Mappings[".tsv"] = "text/tab-separated-values";
            provider.Mappings[".csv"] = "text/csv";
            return provider;
        }

        public static string FromFileName(string fileName)
            => Provider.TryGetContentType(fileName, out var contentType) ? contentType : Default;

        /// <summary>Extension without dot, lower case; null when there is none.</summary>
        public static string? ExtensionOf(string fileName)
        {
            var extension = System.IO.Path.GetExtension(fileName);
            return string.IsNullOrEmpty(extension) ? null : extension.TrimStart('.').ToLowerInvariant();
        }
    }
}
