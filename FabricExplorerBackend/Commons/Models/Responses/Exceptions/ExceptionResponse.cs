using System.Net;

namespace FabricExplorerBackend.Commons.Models.Responses.Exceptions
{
    public sealed record ExceptionResponse(HttpStatusCode statusCode, string description);
}
