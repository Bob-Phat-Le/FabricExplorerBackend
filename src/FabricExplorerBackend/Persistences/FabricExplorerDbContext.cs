using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Persistences.DataSeeding;
using Microsoft.EntityFrameworkCore;

namespace FabricExplorerBackend.Persistences
{
    public class FabricExplorerDbContext(DbContextOptions<FabricExplorerDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Connection> Connections { get; set; }
        public DbSet<FabricOperation> Operations { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
                entity
                    .HasIndex(c => c.WorkspaceId)
                    .IsUnique()
                    .HasFilter("\"DeletedAt\" IS NULL");
            });
            modelBuilder.Entity<FabricOperation>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.HasIndex(o => new { o.Status, o.NextPollAt });
                entity.Property(o => o.ErrorMessage).HasMaxLength(2000);
                entity.Property(o => o.ResourceName).HasMaxLength(256);
            });
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(noti => noti.Id);

                entity.Property(noti => noti.UserId)
                    .IsRequired();

                entity.Property(noti => noti.Type)
                    .IsRequired()
                    .HasMaxLength(Notification.TypeMaxLength);

                entity.Property(noti => noti.Title)
                    .IsRequired()
                    .HasMaxLength(Notification.TitleMaxLength);

                entity.Property(noti => noti.Message)
                    .HasMaxLength(Notification.MessageMaxLength);

                entity.Property(noti => noti.CreatedAt)
                    .IsRequired();

                entity.HasOne(noti => noti.User)
                    .WithMany(usr => usr.Notifications)
                    .HasForeignKey(noti => noti.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(noti => noti.Operation)
                    .WithMany()
                    .HasForeignKey(noti => noti.OperationId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(noti => new { noti.UserId, noti.CreatedAt });
            });

            modelBuilder.SeedData();
        }
    }
}
