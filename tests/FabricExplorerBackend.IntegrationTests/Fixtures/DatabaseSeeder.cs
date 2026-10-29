using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Persistences;

namespace FabricExplorerBackend.IntegrationTests.Fixtures
{
    public sealed class DatabaseSeeder(FabricExplorerDbContext context)
    {
        public async Task SeedConnectionAsync(Connection connection)
        {
            context.Connections.Add(connection);

            await context.SaveChangesAsync();
        }
    }
}
