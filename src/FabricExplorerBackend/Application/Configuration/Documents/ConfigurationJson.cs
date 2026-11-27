using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FabricExplorerBackend.Application.Configuration.Documents
{
    // this class is to define how the configuration document is serialized to JSON, so we can have different options for different scenarios (canonical, file, read, response)
    public static class ConfigurationJson
    {
        /// <summary>Compact, deterministic output used for the integrity hash.</summary>
        public static readonly JsonSerializerOptions Canonical = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>The exported file: human readable.</summary>
        public static readonly JsonSerializerOptions File = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>Unknown fields are skipped so newer minor versions of a file stay readable.</summary>
        public static readonly JsonSerializerOptions Read = new(JsonSerializerDefaults.Web)
        {
            // this is to make sure that if the file has unknown fields, we don't throw an exception, but instead skip them; this is important for forward compatibility, so that if we add new fields in the future, older versions of the application can still read the file without failing
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip
        };

        /// <summary>Same options for responses stored for idempotent replay.</summary>
        public static readonly JsonSerializerOptions Response = new(JsonSerializerDefaults.Web);
    }
}
