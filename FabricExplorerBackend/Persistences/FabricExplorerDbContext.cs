using FabricExplorerBackend.Entities;
using Microsoft.EntityFrameworkCore;

namespace FabricExplorerBackend.Persistences
{
    public class FabricExplorerDbContext(DbContextOptions<FabricExplorerDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Connection> Connections { get; set; }
        public DbSet<FabricOperation> Operations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity
                    .HasOne(u => u.ActiveConnection)
                    .WithMany(c => c.Users)
                    .HasForeignKey(u => u.ActiveConnectionId);
            });
            modelBuilder.Entity<Connection>(entity =>
            {
                entity.HasKey(c => c.Id);
            });
            modelBuilder.Entity<FabricOperation>(entity =>
            {
                entity.HasKey(o => o.Id);
            });

            modelBuilder.SeedData();
        }
    }
}
