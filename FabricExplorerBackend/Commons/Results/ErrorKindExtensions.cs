namespace FabricExplorerBackend.Commons.Results
{
    public static class ErrorKindExtensions
    {
        public static int ToStatusCode(this ErrorKind kind) => kind switch
        {
            ErrorKind.Validation or ErrorKind.BadRequest => 400,
            ErrorKind.Unauthorized => 401,
            ErrorKind.Forbidden => 403,
            ErrorKind.NotFound => 404,
            ErrorKind.Conflict => 409,
            ErrorKind.TooManyRequests => 429,
            ErrorKind.Upstream => 502,
            ErrorKind.Unavailable => 503,
            ErrorKind.Timeout => 504,
            _ => 500
        };
    }

    public static class HttpContextKeys { public const string ErrorCode = "error.code"; }
}
