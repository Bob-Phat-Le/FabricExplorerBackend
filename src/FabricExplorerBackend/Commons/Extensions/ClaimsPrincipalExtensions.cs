using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.Results;
using System.Security.Claims;

namespace FabricExplorerBackend.Commons.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>Returns the id of the authenticated user (the "oid" claim).</summary>
        /// <exception cref="Errors.ApplicationException">The claim is missing or is not a valid Guid.</exception>
        public static Guid GetUserId(this ClaimsPrincipal user)
            => Guid.TryParse(user.FindFirstValue("oid"), out var userId)
                ? userId
                : throw new Errors.ApplicationException(
                    Error.BadRequest(ErrorCodes.User.NotFound, "Current user cannot be determined"));
    }
}
