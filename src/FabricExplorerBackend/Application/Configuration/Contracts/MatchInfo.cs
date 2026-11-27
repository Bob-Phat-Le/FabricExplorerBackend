namespace FabricExplorerBackend.Application.Configuration.Contracts
{
    public sealed record MatchInfo(string Kind, Guid? ExistingId, string? By)
    {
        public const string None = "none";
        public const string Exact = "exact";
        public const string Probable = "probable";

        public static MatchInfo NoMatch { get; } = new(None, null, null);
    }
}
