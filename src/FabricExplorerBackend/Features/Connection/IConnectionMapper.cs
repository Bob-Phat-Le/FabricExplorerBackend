using FabricExplorerBackend.Features.Connection.Contracts;

namespace FabricExplorerBackend.Features.Connection
{
    public interface IConnectionMapper
    {
        ConnectionResponse Map(Domain.Entities.Connection connection);
        IEnumerable<ConnectionResponse> Map(IEnumerable<Domain.Entities.Connection> connectionList);
        void Map(UpdateConnectionRequest request, Domain.Entities.Connection connection);
        Domain.Entities.Connection Map(CreateConnectionRequest request);
    }
}
