using FabricExplorerBackend.Enums;

namespace FabricExplorerBackend.Mappers.Result
{
    public class ResultMapper
    {
        public static int ToHttpStatusCode(ResultStatus status)
        {
            return status switch
            {
                ResultStatus.Success => StatusCodes.Status200OK,
                ResultStatus.NotFound => StatusCodes.Status404NotFound,
                ResultStatus.ValidationError => StatusCodes.Status400BadRequest,
                ResultStatus.Conflict => StatusCodes.Status409Conflict,
                ResultStatus.Unauthorized => StatusCodes.Status401Unauthorized,
                ResultStatus.Forbidden => StatusCodes.Status403Forbidden,

                _ => StatusCodes.Status500InternalServerError
            };
        }
    }
}
