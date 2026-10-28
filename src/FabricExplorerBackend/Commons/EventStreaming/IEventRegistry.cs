namespace FabricExplorerBackend.Commons.EventStreaming
{
    public interface IEventRegistry
    {
        EventStream Create(string? ownerId, string? channel = null);

        IReadOnlyList<EventStream> FindByOwner(string ownerId, string channel);

        bool TryGet(string streamId, out EventStream stream);
        bool Remove(string streamId);
        int RemoveExpired();
    }
}
