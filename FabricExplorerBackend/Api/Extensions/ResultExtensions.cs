using FabricExplorerBackend.Commons.Results;

namespace FabricExplorerBackend.Api.Extensions
{
    public class ResultExtensions
    {
        public static int ToHttpStatusCode(ErrorKind status)
        {
            return status switch
            {
                ErrorKind.Success => StatusCodes.Status200OK,
                ErrorKind.NotFound => StatusCodes.Status404NotFound,
                ErrorKind.ValidationError => StatusCodes.Status400BadRequest,
                ErrorKind.Conflict => StatusCodes.Status409Conflict,
                ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorKind.Forbidden => StatusCodes.Status403Forbidden,

                _ => StatusCodes.Status500InternalServerError
            };
        }
    }
}
