namespace FabricExplorerBackend.Commons.Errors
{
    public static class ErrorCodes
    {
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
        public static class Auth { public const string TokenFailed = "auth.token_failed"; }
        public static class Fabric
        {
            public const string NotFound = "fabric.not_found";
            public const string Forbidden = "fabric.forbidden";
            public const string RateLimited = "fabric.rate_limited";
            public const string RequestFailed = "fabric.request_failed";
            public const string Unavailable = "fabric.unavailable";
            public const string Conflict = "fabric.conflict";
        }
        public static class Sql { public const string QueryFailed = "sql.query_failed"; }
        public static class Cache { public const string Unavailable = "cache.unavailable"; }
        public static class Upstream { public const string Timeout = "upstream.timeout"; }
        public static class Internal { public const string Unexpected = "internal.unexpected"; }
    }

}
