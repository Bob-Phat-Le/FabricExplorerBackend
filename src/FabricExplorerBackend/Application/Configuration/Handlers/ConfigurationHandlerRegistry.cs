namespace FabricExplorerBackend.Application.Configuration.Handlers
{
    public sealed class ConfigurationHandlerRegistry : IConfigurationHandlerRegistry
    {
        private readonly Dictionary<string, IConfigurationEntryHandler> byType;

        public ConfigurationHandlerRegistry(IEnumerable<IConfigurationEntryHandler> handlers)
        {
            Ascending = handlers.OrderBy(h => h.Order).ToList();
            Descending = Ascending.Reverse().ToList();
            byType = Ascending.ToDictionary(h => h.Type, StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyList<IConfigurationEntryHandler> Ascending { get; }
        public IReadOnlyList<IConfigurationEntryHandler> Descending { get; }
        public bool TryGet(string? type, out IConfigurationEntryHandler handler)
        {
            handler = null!;
            return type is not null && byType.TryGetValue(type.Trim(), out handler!);
        }
    }
}
