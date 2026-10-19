using FabricExplorerBackend.Commons.Results;

namespace FabricExplorerBackend.Commons.Errors
{
    public class ApplicationException(Error error, Exception? inner = null) : Exception(error.Message, inner)
    {
        public Error Error { get; } = error;
    }
}
