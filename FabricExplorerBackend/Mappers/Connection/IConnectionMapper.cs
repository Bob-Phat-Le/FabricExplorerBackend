using FabricExplorerBackend.Models.Requests.Connection;
using FabricExplorerBackend.Models.Responses.Connection;

namespace FabricExplorerBackend.Mappers.Connection
{
    public interface IConnectionMapper
    {
        ConnectionResponse Map(Entities.Connection connection);
        IEnumerable<ConnectionResponse> Map(IEnumerable<Entities.Connection> connectionList);
        void Map(UpdateConnectionRequest request, Entities.Connection connection);
        Entities.Connection Map(CreateConnectionRequest request);
    }
}
