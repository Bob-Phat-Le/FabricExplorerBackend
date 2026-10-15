namespace FabricExplorerBackend.Infrastructures.Options
{
    public class ApiOptions
    {
        public const string SectionName = "Api";

        public HeaderOptions Header { get; init; } = null!;
    }

    public class HeaderOptions
    {
        public const string SectionName = "Api:Header";

        public string ConnectionId { get; init; } = "X-Connection-Id";
    }
}
