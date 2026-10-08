using FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.Helpers
{
    public interface ICredentialHelper
    {
        Task<Connection?> GetCredentialWithConnectionId(Guid connectionId);
    }
}
