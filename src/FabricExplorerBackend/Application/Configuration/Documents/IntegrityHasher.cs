using System.Security.Cryptography;
using System.Text.Json;

namespace FabricExplorerBackend.Application.Configuration.Documents
{
    /// <summary>Detects a corrupted or hand-edited file. This is not a security feature.</summary>
    public sealed class Sha256IntegrityHasher : IIntegrityHasher
    {
        public ConfigurationIntegrity Compute(ConfigurationDocument document)
        {
            var payload = new IntegrityPayload(
                document.Connections ?? new List<ConnectionEntry>(),
                document.SavedQueries ?? new List<SavedQueryEntry>(),
                document.Preferences);

            var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, ConfigurationJson.Canonical);

            return new ConfigurationIntegrity
            {
                Algorithm = ConfigurationFormat.IntegrityAlgorithm,
                Value = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()
            };
        }

        public bool Verify(ConfigurationDocument document)
        {
            if (document.Integrity is null)
                return true;

            if (!string.Equals(document.Integrity.Algorithm, ConfigurationFormat.IntegrityAlgorithm, StringComparison.OrdinalIgnoreCase))
                return false;

            return string.Equals(document.Integrity.Value, Compute(document).Value, StringComparison.OrdinalIgnoreCase);
        }
    }
}
