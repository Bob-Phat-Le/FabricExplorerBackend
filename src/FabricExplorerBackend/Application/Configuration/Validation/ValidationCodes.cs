namespace FabricExplorerBackend.Application.Configuration.Validation
{
    /// <summary>Error codes of the entry validators. They are part of the preview / import response.</summary>
    public static class ValidationCodes
    {
        public const string Required = "required";
        public const string TooLong = "too_long";
        public const string InvalidKey = "invalid_key";
        public const string InvalidGuid = "invalid_guid";
        public const string InvalidTokenEndpoint = "invalid_token_endpoint";
        public const string TenantMismatch = "tenant_mismatch";
        public const string InvalidLandingZone = "invalid_landing_zone";
        public const string InvalidItemType = "invalid_item_type";
        public const string InvalidLanguage = "invalid_language";
        public const string InvalidMaxRows = "invalid_max_rows";
        public const string ReferenceUnresolved = "reference_unresolved";
    }
}
