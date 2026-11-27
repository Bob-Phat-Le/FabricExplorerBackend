using FabricExplorerBackend.Commons.Extensions;
using FabricExplorerBackend.Commons.Security;

namespace FabricExplorerBackend.Infrastructures.Identity
{
    public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
    {
        public Guid Id => httpContextAccessor.HttpContext!.User.GetUserId();
    }
}
