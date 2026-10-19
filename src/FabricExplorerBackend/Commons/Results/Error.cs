namespace FabricExplorerBackend.Commons.Results
{
    public sealed record Error(ErrorKind Kind, string Code, string Message)
    {
        public static Error NotFound(string code, string message) => new(ErrorKind.NotFound, code, message);
        public static Error BadRequest(string code, string message) => new(ErrorKind.BadRequest, code, message);
        public static Error Conflict(string code, string message) => new(ErrorKind.Conflict, code, message);
        public static Error Forbidden(string code, string message) => new(ErrorKind.Forbidden, code, message);
    }
}
