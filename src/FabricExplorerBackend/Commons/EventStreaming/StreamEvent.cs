namespace FabricExplorerBackend.Commons.EventStreaming
{
    public sealed record StreamEvent(long Id, string Name, string Data)
    {
        public static readonly StreamEvent Heartbeat = new(0, ":heartbeat", string.Empty);
    }

    public static class StreamEventNames
    {
        public const string Completed = "completed";
        public const string Error = "error";
    }

    public sealed record StreamErrorPayload(string Code, string Message);
}
