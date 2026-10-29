namespace FabricExplorerBackend.Commons.EventStreaming
{
    public sealed class EventStream : IDisposable
    {
        private readonly object gate = new();
        private readonly List<StreamEvent> events = [];
        private readonly CancellationTokenSource lifetime = new();
        private readonly int maxEvents;
        private TaskCompletionSource signal = NewSignal();

        public EventStream(string id, string? ownerId, int maxEvents, DateTimeOffset createdAt, string? channel = null)
        {
            Id = id;
            OwnerId = ownerId;
            Channel = channel;
            CreatedAt = createdAt;
            this.maxEvents = maxEvents;
            Lifetime = lifetime.Token;
        }

        public string Id { get; }
        public string? OwnerId { get; }

        public string? Channel { get; }
        public DateTimeOffset CreatedAt { get; }
        public DateTimeOffset? CompletedAt { get; private set; }
        public bool IsCompleted { get { lock (gate) return CompletedAt.HasValue; } }
        public long LastEventId { get { lock (gate) return events.Count; } }

        public CancellationToken Lifetime { get; }

        public bool TryAppend(string name, string data, DateTimeOffset now)
        {
            TaskCompletionSource toWake;
            lock (gate) // [self-comment] The reader and the producer might run in different thread, so to be save when either reader or producer needs to work with event list, this needs to be locked.
            {
                if (CompletedAt.HasValue) return false;
                var isTerminal = name is StreamEventNames.Completed or StreamEventNames.Error;
                if (!isTerminal && events.Count >= maxEvents - 1) return false;

                events.Add(new StreamEvent(events.Count + 1, name, data));
                if (isTerminal) CompletedAt = now;

                toWake = signal;
                signal = NewSignal();
            }
            toWake.TrySetResult(); // [self-comment] I might need to read again about how the OS work for this. This just tell the awaiter to wakeup (currently in this situation, the reader is the only awaiter)
            return true;
        }

        public async IAsyncEnumerable<StreamEvent> ReadAsync(
            long afterEventId,
            TimeSpan heartbeatInterval,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var next = Math.Max(0, afterEventId);

            while (true)
            {
                StreamEvent[] batch;
                bool completed;
                Task wait;

                lock (gate) // [self-comment] The reader and the producer might run in different thread, so to be save when either reader or producer needs to work with event list, this needs to be locked.
                {
                    batch = next < events.Count ? events.GetRange((int)next, events.Count - (int)next).ToArray() : [];
                    completed = CompletedAt.HasValue;
                    wait = signal.Task;
                }

                foreach (var item in batch)
                {
                    yield return item; // [self-comment] Still, I still don't really understand how 'yield' works. Here is just mean that this function will stop here until the caller need the next item.
                    next = item.Id;
                }

                if (completed && batch.Length == 0)
                    yield break;

                if (batch.Length == 0)
                {
                    var heartbeat = false;
                    try
                    {
                        await wait.WaitAsync(heartbeatInterval, cancellationToken);
                    }
                    catch (TimeoutException)
                    {
                        heartbeat = true;
                    }

                    if (heartbeat)
                        yield return StreamEvent.Heartbeat;
                }
            }
        }

        public void Dispose()
        {
            try { lifetime.Cancel(); } catch (ObjectDisposedException) { }
            lifetime.Dispose();
        }

        private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
