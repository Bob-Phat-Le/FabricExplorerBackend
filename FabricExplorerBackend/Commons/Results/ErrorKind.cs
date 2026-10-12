namespace FabricExplorerBackend.Commons.Results
{
    public enum ErrorKind
    {
        Success,
        NotFound,
        BadRequest,
        ValidationError,
        Conflict,
        Unauthorized,
        Forbidden,
        InternalError
    }
}
