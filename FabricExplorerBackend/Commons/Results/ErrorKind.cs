namespace FabricExplorerBackend.Commons.Results
{
    public enum ErrorKind
    {
        Validation,
        BadRequest,
        Unauthorized,
        Forbidden,
        NotFound,
        Conflict,
        TooManyRequests,
        Internal,
        Upstream,
        Unavailable,
        Timeout
    }
}
