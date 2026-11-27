namespace FabricExplorerBackend.Application.Configuration.Contracts
{
    /// <summary>The Idempotency-Key header of an import, normalised: trimmed, and null when blank.</summary>
    public sealed record ImportIdempotencyKey(string? Value)
    {
        public static ImportIdempotencyKey From(string? header)
            => new(string.IsNullOrWhiteSpace(header) ? null : header.Trim());
    }
}
