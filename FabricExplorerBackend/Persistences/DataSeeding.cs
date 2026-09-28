using Microsoft.EntityFrameworkCore;
using FabricExplorerBackend.Entities;

namespace FabricExplorerBackend.Persistences
{
    public static class DataSeeding
    {
        public static void SeedData(this ModelBuilder modelBuilder)
        {
            // Fixed IDs
            Guid userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            Guid userPreferenceId = Guid.Parse("22222222-2222-2222-2222-222222222222");

            Guid connectionId1 = Guid.Parse("33333333-3333-3333-3333-333333333333");
            Guid connectionId2 = Guid.Parse("44444444-4444-4444-4444-444444444444");

            Guid tenantId = Guid.Parse(
                "72f988bf-86f1-41af-91ab-2d7cd011db47"
            );

            Guid clientId1 = Guid.Parse(
                "8f3e1a2b-9c4d-4e5f-8a1b-3c5d7e9f2a4b"
            );

            Guid clientId2 = Guid.Parse(
                "3a2b1c4d-8e7f-6a5b-4c3d-2e1f0a9b8c7d"
            );

            // Fixed timestamp
            DateTimeOffset createdAt =
                new DateTimeOffset(2026, 9, 28, 0, 0, 0, TimeSpan.Zero);

            User seededUser = new()
            {
                Id = userId,
                UserName = "John Doe",
                Email = "john.doe@company.com",
                CreatedAt = createdAt,
                UserPreferenceId = userPreferenceId
            };

            UserPreference seededUserPreference = new()
            {
                Id = userPreferenceId,
                CreatedAt = createdAt,
                UserId = userId,
                LastUsedConnectionId = connectionId1
            };

            Connection seededConnection1 = new()
            {
                Id = connectionId1,
                Name = "Production Fabric",
                CreatedAt = createdAt,
                ClientId = clientId1,
                WorkspaceId = Guid.Parse(
                    "12345678-abcd-1234-abcd-1234567890ab"
                ),
                TenantId = tenantId,
                ClientSecret = "8f3e1a2b-9c4d-4e5f-8a1b-3c5d7e9f2a4b"
            };

            Connection seededConnection2 = new()
            {
                Id = connectionId2,
                Name = "Staging Analytics",
                CreatedAt = createdAt,
                ClientId = clientId2,
                WorkspaceId = Guid.Parse(
                    "87654321-abcd-4321-abcd-0987654321ba"
                ),
                ClientSecret = "secret",
                TenantId = tenantId
            };

            modelBuilder.Entity<User>()
                .HasData(seededUser);

            modelBuilder.Entity<UserPreference>()
                .HasData(seededUserPreference);

            modelBuilder.Entity<Connection>()
                .HasData(
                    seededConnection1,
                    seededConnection2
                );
        }
    }
}
