using System.Text.RegularExpressions;

namespace FabricExplorerBackend.Application.Configuration.Validation
{
    public static class TokenEndpoints
    {
        public const string Authority = "https://login.microsoftonline.com";

        private static readonly Regex PathPattern = new(
            @"^/(?<tenant>[^/]+)/oauth2(/v2\.0)?/token/?$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));

        public static string ForTenant(Guid tenantId) => $"{Authority}/{tenantId:D}/oauth2/v2.0/token";

        public static bool IsHttpsUrl(string? text)
            => Uri.TryCreate(text?.Trim(), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;

        /// <summary>Reads the tenant segment of a /{tenant}/oauth2[/v2.0]/token URL; false when the URL does not have that shape.</summary>
        public static bool TryGetTenantSegment(string? text, out string tenant)
        {
            tenant = string.Empty;

            if (!Uri.TryCreate(text?.Trim(), UriKind.Absolute, out var uri))
                return false;

            var match = PathPattern.Match(uri.AbsolutePath);
            if (!match.Success)
                return false;

            tenant = match.Groups["tenant"].Value;
            return true;
        }
    }
}
