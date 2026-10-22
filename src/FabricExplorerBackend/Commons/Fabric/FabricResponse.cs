namespace FabricExplorerBackend.Commons.Fabric
{
    public sealed class FabricResponse<T>
    {
        public int Status { get; init; }
        public IReadOnlyDictionary<string, string[]> Headers { get; init; } = new Dictionary<string, string[]>();
        public T? Data { get; init; }
    }
}
