using FabricExplorerBackend.Commons.Models.Requests.Connection;
using FabricExplorerBackend.Commons.Models.Responses.Connection;
using FabricExplorerBackend.Features.Connection;
using Riok.Mapperly.Abstractions;

namespace FabricExplorerBackend.Mappers
{
    [Mapper(AllowNullPropertyAssignment = false)]
    public partial class ConnectionMapper : IConnectionMapper
    {

        [MapperIgnoreSource(nameof(Domain.Entities.Connection.CreatedAt))]
        [MapperIgnoreSource(nameof(Domain.Entities.Connection.UpdatedAt))]
        [MapperIgnoreSource(nameof(Domain.Entities.Connection.DeletedAt))]
        [MapperIgnoreSource(nameof(Domain.Entities.Connection.IsDeleted))]
        [MapperIgnoreSource(nameof(Domain.Entities.Connection.CreatedBy))]
        [MapperIgnoreSource(nameof(Domain.Entities.Connection.UpdatedBy))]
        public partial ConnectionResponse Map(Domain.Entities.Connection connection);
        public partial IEnumerable<ConnectionResponse> Map(IEnumerable<Domain.Entities.Connection> connectionList);

        [MapperIgnoreTarget(nameof(Domain.Entities.Connection.CreatedAt))]
        [MapperIgnoreTarget(nameof(Domain.Entities.Connection.UpdatedAt))]
        [MapperIgnoreTarget(nameof(Domain.Entities.Connection.DeletedAt))]
        [MapperIgnoreTarget(nameof(Domain.Entities.Connection.IsDeleted))]
        [MapperIgnoreTarget(nameof(Domain.Entities.Connection.CreatedBy))]
        [MapperIgnoreTarget(nameof(Domain.Entities.Connection.UpdatedBy))]
        public partial void Map(UpdateConnectionRequest request, Domain.Entities.Connection connection);

        [MapperIgnoreTarget(nameof(Domain.Entities.Connection.CreatedAt))]
        [MapperIgnoreTarget(nameof(Domain.Entities.Connection.UpdatedAt))]
        [MapperIgnoreTarget(nameof(Domain.Entities.Connection.DeletedAt))]
        [MapperIgnoreTarget(nameof(Domain.Entities.Connection.IsDeleted))]
        [MapperIgnoreTarget(nameof(Domain.Entities.Connection.CreatedBy))]
        [MapperIgnoreTarget(nameof(Domain.Entities.Connection.UpdatedBy))]
        public partial Domain.Entities.Connection Map(CreateConnectionRequest request);
    }
}
