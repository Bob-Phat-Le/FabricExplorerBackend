using FabricExplorerBackend.Application.Connection.Contracts;
using FabricExplorerBackend.Mappers;
using Entities = FabricExplorerBackend.Domain.Entities;

namespace FabricExplorerBackend.UnitTests.Features.Connection
{
    public class ConnectionMapper_Tests
    {
        private readonly ConnectionMapper _mapper = new();

        [Fact]
        public void Map_ConnectionToConnectionResponse_MapsCorrectly()
        {
            var entity = new Entities.Connection
            {
                Id = Guid.NewGuid(),
                Name = "Test Conn",
                ClientId = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                WorkspaceId = Guid.NewGuid()
            };

            var response = _mapper.Map(entity);

            Assert.NotNull(response);
            Assert.Equal(entity.Id, response.Id);
            Assert.Equal(entity.Name, response.Name);
            Assert.Equal(entity.ClientId, response.ClientId);
            Assert.Equal(entity.TenantId, response.TenantId);
            Assert.Equal(entity.WorkspaceId, response.WorkspaceId);
        }

        [Fact]
        public void Map_ConnectionListToResponseList_MapsAllItems()
        {
            var list = new List<Entities.Connection>
            {
                new() { Id = Guid.NewGuid(), Name = "Conn 1" },
                new() { Id = Guid.NewGuid(), Name = "Conn 2" }
            };

            var responses = _mapper.Map(list).ToList();

            Assert.Equal(2, responses.Count);
            Assert.Equal("Conn 1", responses[0].Name);
            Assert.Equal("Conn 2", responses[1].Name);
        }

        [Fact]
        public void Map_CreateConnectionRequestToConnection_MapsCorrectly()
        {
            var request = new CreateConnectionRequest
            {
                Name = "New Conn",
                ClientId = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                WorkspaceId = Guid.NewGuid(),
                ClientSecret = "plain-secret"
            };

            var entity = _mapper.Map(request);

            Assert.NotNull(entity);
            Assert.Equal(request.Name, entity.Name);
            Assert.Equal(request.ClientId, entity.ClientId);
            Assert.Equal(request.TenantId, entity.TenantId);
            Assert.Equal(request.WorkspaceId, entity.WorkspaceId);
            Assert.Equal(request.ClientSecret, entity.ClientSecret);
        }

        [Fact]
        public void Map_UpdateConnectionRequest_UpdatesExistingEntity()
        {
            var entity = new Entities.Connection
            {
                Id = Guid.NewGuid(),
                Name = "Old Name",
                WorkspaceId = Guid.NewGuid(),
                ClientSecret = "old-secret"
            };

            var newWorkspaceId = Guid.NewGuid();
            var request = new UpdateConnectionRequest
            {
                Name = "Updated Name",
                WorkspaceId = newWorkspaceId,
                ClientSecret = "new-secret"
            };

            _mapper.Map(request, entity);

            Assert.Equal("Updated Name", entity.Name);
            Assert.Equal(newWorkspaceId, entity.WorkspaceId);
            Assert.Equal("new-secret", entity.ClientSecret);
        }
    }
}
