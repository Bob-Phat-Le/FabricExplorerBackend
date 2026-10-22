namespace FabricExplorerBackend.Commons.EventStreaming
{
    public interface IEventRegistry
    {
        EventStream Create(string? ownerId);
        bool TryGet(string streamId, out EventStream stream);
        bool Remove(string streamId);
        int RemoveExpired();
    }
}
