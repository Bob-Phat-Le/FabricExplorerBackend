using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.Results;
using System.Text.RegularExpressions;

namespace FabricExplorerBackend.Infrastructures.LandingZone
{
    public static partial class LandingZonePaths
    {
        public const int MaxPathLength = 1024;
        public const int MaxNameLength = 255;

        [GeneratedRegex("[\\\\/:*?\"<>|]")]
        private static partial Regex InvalidNameChars();

        public static string Normalize(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;

            if (path.Contains('\\'))
                throw Invalid("Path must use '/' as separator.");

            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            foreach (var segment in segments)
            {
                if (segment is "." or "..")
                    throw Invalid("Path must not contain '.' or '..' segments.");
                if (segment.Length > MaxNameLength)
                    throw Invalid($"A path segment is longer than {MaxNameLength} characters.");
                if (segment.Any(char.IsControl))
                    throw Invalid("Path contains control characters.");
            }

            var normalized = string.Join('/', segments);
            if (normalized.Length > MaxPathLength)
                throw Invalid($"Path is longer than {MaxPathLength} characters.");

            return normalized;
        }

        public static string ValidateName(string? name)
        {
            var trimmed = (name ?? string.Empty).Trim();

            if (trimmed.Length == 0)
                throw Invalid("Name is required.");
            if (trimmed.Length > MaxNameLength)
                throw Invalid($"Name is longer than {MaxNameLength} characters.");
            if (trimmed is "." or "..")
                throw Invalid("Name is not allowed.");
            if (InvalidNameChars().IsMatch(trimmed) || trimmed.Any(char.IsControl))
                throw Invalid("Name must not contain any of \\ / : * ? \" < > |");

            return trimmed;
        }

        public static string Combine(string parent, string child)
            => string.IsNullOrEmpty(parent) ? child : string.IsNullOrEmpty(child) ? parent : $"{parent}/{child}";

        public static string ParentOf(string path)
        {
            var index = path.LastIndexOf('/');
            return index < 0 ? string.Empty : path[..index];
        }

        public static string NameOf(string path)
        {
            var index = path.LastIndexOf('/');
            return index < 0 ? path : path[(index + 1)..];
        }

        public static bool IsSameOrDescendant(string candidate, string ancestor)
            => string.IsNullOrEmpty(ancestor)
               || candidate.Equals(ancestor, StringComparison.Ordinal)
               || candidate.StartsWith(ancestor + "/", StringComparison.Ordinal);

        public static string Display(string path) => "/" + path;

        private static Commons.Errors.ApplicationException Invalid(string message)
            => new(Error.BadRequest(ErrorCodes.LandingZone.InvalidPath, message));
    }
}
