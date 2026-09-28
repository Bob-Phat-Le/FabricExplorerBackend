using FabricExplorerBackend.Persistences;
using Microsoft.EntityFrameworkCore;

namespace FabricExplorerBackend.Extensions
{
    public static class AppMigrationExtensions
    {
        public static async Task MigrateDatabaseAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FabricExplorerDbContext>();
            db.Database.Migrate();
        }
    }
}
