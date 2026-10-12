using System.Diagnostics;

namespace FabricExplorerBackend.Api.Extensions
{
    public static class HttpContextLogExtensions
    {
        public static string GetTraceId(this HttpContext http)
            => Activity.Current?.TraceId.ToString() ?? http.TraceIdentifier;

        public static Dictionary<string, object?> GetLogProperties(this HttpContext http)
        {
            var props = new Dictionary<string, object?>
            {
                ["TraceId"] = http.GetTraceId(),
                ["UserId"] = http.User.FindFirst("oid")?.Value,
                ["ConnectionId"] = http.Request.Headers["X-Connection-Id"].FirstOrDefault()
            };
            foreach (var (key, value) in http.Request.RouteValues)   // workspaceId, lakehouseId...
                if (key is not ("controller" or "action")) props[key] = value;
            return props;
        }
    }
}
