using FabricExplorerBackend.Entities;
using Microsoft.EntityFrameworkCore;

namespace FabricExplorerBackend.Persistences
{
    public class FabricExplorerDbContext(DbContextOptions<FabricExplorerDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<UserPreference> UserPreferences { get; set; }
        public DbSet<Connection> Connections { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints
            modelBuilder.Entity<UserPreference>(entity =>
            {
                entity.HasKey(up => up.Id);
                entity
                    .HasOne(up => up.User)
                    .WithOne(u => u.UserPreference)
                    .HasForeignKey<UserPreference>(up => up.UserId);
                entity
                    .HasOne(up => up.LastUsedConnection)
                    .WithMany(c => c.UserPreference)
                    .HasForeignKey(up => up.LastUsedConnectionId);
            });
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
            });
            modelBuilder.Entity<Connection>(entity =>
            {
                entity.HasKey(c => c.Id);
            });

            modelBuilder.SeedData();
        }
    }
}
