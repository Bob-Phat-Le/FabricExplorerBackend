namespace FabricExplorerBackend.Commons.EventStreaming
{
    public interface IEventPublisher
    {
        bool Publish<T>(string streamId, string eventName, T payload);

        int PublishToOwner<T>(string ownerId, string channel, string eventName, T payload);

        void Complete(string streamId);
        void Fail(string streamId, string code, string message);
    }
}
