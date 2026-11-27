namespace FabricExplorerBackend.Application.Configuration.Validation
{
    public static class ConfigurationGuid
    {
        /// <summary>Parses a GUID and rejects the empty GUID.</summary>
        public static bool TryParse(string? text, out Guid value)
            => Guid.TryParse(text, out value) && value != Guid.Empty;
    }
}
