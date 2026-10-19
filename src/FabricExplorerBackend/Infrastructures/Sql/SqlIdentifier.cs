namespace FabricExplorerBackend.Infrastructures.Sql
{
    public static class SqlIdentifier
    {
        private const int MaxLength = 128;

        public static string Quote(params string[] parts)
        {
            if (parts == null || parts.Length == 0)
                throw new ArgumentException("At least one identifier part is required.", nameof(parts));

            return string.Join(".", parts.Select(QuotePart));
        }

        private static string QuotePart(string part)
        {
            if (string.IsNullOrWhiteSpace(part))
                throw new ArgumentException("SQL identifier must not be empty.");
            if (part.Length > MaxLength)
                throw new ArgumentException($"SQL identifier must not exceed {MaxLength} characters.");
            if (part.Contains('\0'))
                throw new ArgumentException("SQL identifier must not contain null characters.");

            return "[" + part.Replace("]", "]]") + "]";
        }
    }
}
