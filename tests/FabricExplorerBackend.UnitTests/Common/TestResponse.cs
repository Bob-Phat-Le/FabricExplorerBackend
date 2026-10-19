using Azure;
using Azure.Core;

namespace FabricExplorerBackend.UnitTests.Common
{
    public class TestResponse : Response
    {
        private readonly int _status;
        private readonly Dictionary<string, string> _headers;

        public TestResponse(int status, Dictionary<string, string>? headers = null)
        {
            _status = status;
            _headers = headers != null
                ? new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public override int Status => _status;
        public override string ReasonPhrase => string.Empty;
        public override Stream? ContentStream { get; set; }
        public override string ClientRequestId { get; set; } = string.Empty;

        public override void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        protected override bool TryGetHeader(string name, [NotNullWhen(true)] out string? value)
            => _headers.TryGetValue(name, out value);

        protected override bool TryGetHeaderValues(string name, [NotNullWhen(true)] out IEnumerable<string>? values)
        {
            if (_headers.TryGetValue(name, out var val))
            {
                values = new[] { val };
                return true;
            }
            values = null;
            return false;
        }

        protected override bool ContainsHeader(string name) => _headers.ContainsKey(name);

        protected override IEnumerable<HttpHeader> EnumerateHeaders()
            => _headers.Select(kv => new HttpHeader(kv.Key, kv.Value));
    }
}
