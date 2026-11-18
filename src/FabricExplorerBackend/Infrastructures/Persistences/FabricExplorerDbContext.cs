using FabricExplorerBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FabricExplorerBackend.Infrastructures.Persistences
{
    public class FabricExplorerDbContext(DbContextOptions<FabricExplorerDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Connection> Connections { get; set; }
        public DbSet<FabricOperation> Operations { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<LandingZoneConfig> LandingZoneConfigs { get; set; }
        public DbSet<LandingZoneOperation> LandingZoneOperations { get; set; }

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

            modelBuilder.Entity<LandingZoneConfig>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.StorageEndpoint)
                    .IsRequired()
                    .HasMaxLength(LandingZoneConfig.EndpointMaxLength);

                entity.Property(c => c.FileSystem)
                    .IsRequired()
                    .HasMaxLength(LandingZoneConfig.FileSystemMaxLength);

                entity.Property(c => c.RootPath)
                    .IsRequired()
                    .HasMaxLength(LandingZoneConfig.RootPathMaxLength);

                // One Landing Zone per connection.
                entity.HasIndex(c => c.ConnectionId).IsUnique();

                entity.HasOne<Connection>()
                    .WithMany()
                    .HasForeignKey(c => c.ConnectionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<LandingZoneOperation>(entity =>
            {
                entity.HasKey(o => o.Id);

                entity.Property(o => o.Operation).IsRequired().HasMaxLength(LandingZoneOperation.OperationMaxLength);
                entity.Property(o => o.Item).IsRequired().HasMaxLength(LandingZoneOperation.ItemMaxLength);
                entity.Property(o => o.Path).IsRequired().HasMaxLength(LandingZoneOperation.PathMaxLength);
                entity.Property(o => o.Status).IsRequired().HasMaxLength(LandingZoneOperation.StatusMaxLength);
                entity.Property(o => o.Details).HasMaxLength(LandingZoneOperation.DetailsMaxLength);

                entity.HasIndex(o => new { o.ConnectionId, o.CreatedAt });

                entity.HasOne<Connection>()
                    .WithMany()
                    .HasForeignKey(o => o.ConnectionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.SeedData();
        }
    }
}
