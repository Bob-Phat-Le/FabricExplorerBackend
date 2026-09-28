using FabricExplorerBackend.Mappers.Connection;
using FabricExplorerBackend.Models.Requests.Connection;
using FabricExplorerBackend.Models.Responses.Connection;
using Riok.Mapperly.Abstractions;

namespace FabricExplorerBackend.Mappers
{
    [Mapper]
    public partial class ConnectionMapper : IConnectionMapper
    {

        [MapperIgnoreSource(nameof(Entities.Connection.CreatedAt))]
        [MapperIgnoreSource(nameof(Entities.Connection.UpdatedAt))]
        [MapperIgnoreSource(nameof(Entities.Connection.DeletedAt))]
        [MapperIgnoreSource(nameof(Entities.Connection.IsDeleted))]
        [MapperIgnoreSource(nameof(Entities.Connection.CreatedBy))]
        [MapperIgnoreSource(nameof(Entities.Connection.UpdatedBy))]
        [MapperIgnoreSource(nameof(Entities.Connection.UserPreference))]
        public partial ConnectionResponse Map(Entities.Connection connection);
        public partial IEnumerable<ConnectionResponse> Map(IEnumerable<Entities.Connection> connectionList);

        [MapperIgnoreTarget(nameof(Entities.Connection.CreatedAt))]
        [MapperIgnoreTarget(nameof(Entities.Connection.UpdatedAt))]
        [MapperIgnoreTarget(nameof(Entities.Connection.DeletedAt))]
        [MapperIgnoreTarget(nameof(Entities.Connection.IsDeleted))]
        [MapperIgnoreTarget(nameof(Entities.Connection.CreatedBy))]
        [MapperIgnoreTarget(nameof(Entities.Connection.UpdatedBy))]
        [MapperIgnoreTarget(nameof(Entities.Connection.UserPreference))]
        public partial void Map(UpdateConnectionRequest request, Entities.Connection connection);

        [MapperIgnoreTarget(nameof(Entities.Connection.CreatedAt))]
        [MapperIgnoreTarget(nameof(Entities.Connection.UpdatedAt))]
        [MapperIgnoreTarget(nameof(Entities.Connection.DeletedAt))]
        [MapperIgnoreTarget(nameof(Entities.Connection.IsDeleted))]
        [MapperIgnoreTarget(nameof(Entities.Connection.CreatedBy))]
        [MapperIgnoreTarget(nameof(Entities.Connection.UpdatedBy))]
        [MapperIgnoreTarget(nameof(Entities.Connection.UserPreference))]
        public partial Entities.Connection Map(CreateConnectionRequest request);
    }
}
