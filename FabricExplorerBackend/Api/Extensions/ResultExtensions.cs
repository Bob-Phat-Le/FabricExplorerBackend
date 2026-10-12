using FabricExplorerBackend.Api.Contracts;
using FabricExplorerBackend.Commons.Results;
using Microsoft.AspNetCore.Mvc;

namespace FabricExplorerBackend.Api.Extensions
{
    public static class ResultExtensions
    {
        public static ActionResult<ApiResponse<T>> ToActionResult<T>(this Result<T> result, ControllerBase c)
        {
            if (result.IsSuccess) return c.Ok(ApiResponse<T>.Ok(result.Data));

            var e = result.Errors[0];
            var status = e.Kind.ToStatusCode();
            c.HttpContext.Items[HttpContextKeys.ErrorCode] = e.Code;   // để request log hiện ErrorCode
            return c.StatusCode(status, ApiResponse<T>.Fail(status, new ApiError(e.Code, e.Message, c.HttpContext.TraceIdentifier)));
        }
    }
}
