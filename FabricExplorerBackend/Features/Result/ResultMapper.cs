using FabricExplorerBackend.Domain.Enums;

namespace FabricExplorerBackend.Features.Result
{
    public class ResultMapper
    {
        public static int ToHttpStatusCode(ResultStatus status)
        {
            return status switch
            {
                ResultStatus.Success => StatusCodes.Status200OK,
                ResultStatus.NotFound => StatusCodes.Status404NotFound,
                ResultStatus.BadRequest => StatusCodes.Status400BadRequest,
                ResultStatus.ValidationError => StatusCodes.Status400BadRequest,
                ResultStatus.Conflict => StatusCodes.Status409Conflict,
                ResultStatus.Unauthorized => StatusCodes.Status401Unauthorized,
                ResultStatus.Forbidden => StatusCodes.Status403Forbidden,

                _ => StatusCodes.Status500InternalServerError
            };
        }
    }
}
