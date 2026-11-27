namespace FabricExplorerBackend.Commons.Errors
{
    public static class ErrorCodes
    {
        public static class Context
        {
            public const string BadRequest = "context.bad_request";
        }
        public static class User
        {
            public const string NotFound = "user.not_found";
        }
        public static class Operation
        {
            public const string NotFound = "operation.not_found";
            public const string TimedOut = "operation.timed_out";
            public const string PollGaveUp = "operation.poll_gave_up";
            public const string FabricLost = "operation.fabric_not_found";
            public const string FabricFailed = "operation.fabric_failed";
            public const string UntrustedUrl = "operation.untrusted_url";
        }
        public static class Connection
        {
            public const string NotFound = "connection.not_found";
            public const string Duplicated = "connection.duplicated";
            public const string BadRequest = "connection.bad_request";
        }
        public static class SavedQuery
        {
            public const string NotFound = "saved_query.not_found";
            public const string Duplicated = "saved_query.duplicated";
            public const string BadRequest = "saved_query.bad_request";
        }
        public static class Configuration
        {
            public const string InvalidFormat = "configuration.invalid_format";
            public const string UnsupportedVersion = "configuration.unsupported_version";
            public const string TooLarge = "configuration.too_large";
            public const string IntegrityMismatch = "configuration.integrity_mismatch";
            public const string ReferenceUnresolved = "configuration.reference_unresolved";
            public const string InvalidMode = "configuration.invalid_mode";
            public const string InvalidSelection = "configuration.invalid_selection";
            public const string InvalidMapping = "configuration.invalid_mapping";
            public const string InvalidIdempotencyKey = "configuration.invalid_idempotency_key";
            public const string RolledBack = "configuration.rolled_back";
            public const string EntryFailed = "configuration.entry_failed";
        }
        public static class Auth
        {
            public const string TokenFailed = "auth.token_failed";
            public const string Unprotected = "auth.secret_unprotected";
        }
        public static class Fabric
        {
            public const string NotFound = "fabric.not_found";
            public const string Forbidden = "fabric.forbidden";
            public const string RateLimited = "fabric.rate_limited";
            public const string RequestFailed = "fabric.request_failed";
            public const string Unavailable = "fabric.unavailable";
            public const string Conflict = "fabric.conflict";
        }
        public static class EventStream
        {
            public const string NotFound = "event_stream.not_found";
            public const string LimitReached = "event_stream.limit_reached";
        }

        public static class Notification
        {
            public const string NotFound = "notification.not_found";
        }
        public static class Catalog
        {
            public const string BadRequest = "catalog.bad_request";
            public const string TableNotFound = "catalog.table_not_found";
        }
        public static class LandingZone
        {
            public const string NotConfigured = "landing_zone.not_configured";
            public const string BadRequest = "landing_zone.bad_request";
            public const string InvalidPath = "landing_zone.invalid_path";
            public const string PathNotFound = "landing_zone.path_not_found";
            public const string AlreadyExists = "landing_zone.already_exists";
            public const string UnsupportedEndpoint = "landing_zone.unsupported_endpoint";
        }
        public static class MetadataGenerate
        {
            public const string InvalidXsd = "metadata_generate.invalid_xsd";
            public const string InvalidMetadata = "metadata_generate.invalid_metadata";
            public const string MetadataAlreadyExists = "metadata_generate.metadata_already_exists";
        }
        public static class Sql { public const string QueryFailed = "sql.query_failed"; }
        public static class Cache { public const string Unavailable = "cache.unavailable"; }
        public static class Upstream { public const string Timeout = "upstream.timeout"; }
        public static class Internal { public const string Unexpected = "internal.unexpected"; }
    }

}
