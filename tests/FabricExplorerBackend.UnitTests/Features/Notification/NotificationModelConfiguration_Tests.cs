using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Infrastructures.Persistences;
using Microsoft.EntityFrameworkCore;

namespace FabricExplorerBackend.UnitTests.Features.Notification
{
    public class NotificationModelConfiguration_Tests
    {
        private readonly FabricExplorerDbContext _context;

        public NotificationModelConfiguration_Tests()
        {
            var options = new DbContextOptionsBuilder<FabricExplorerDbContext>()
                .UseNpgsql("Host=localhost;Database=dummy_test;Username=dummy;Password=dummy")
                .Options;

            _context = new FabricExplorerDbContext(options);
        }

        [Fact]
        public void DbContext_HasNotificationsDbSet()
        {
            Assert.NotNull(_context.Notifications);
        }

        [Fact]
        public void NotificationEntity_ModelConfiguration_IsValid()
        {
            var model = _context.Model;
            var entityType = model.FindEntityType(typeof(Domain.Entities.Notification));

            Assert.NotNull(entityType);

            // Primary Key on Id
            var primaryKey = entityType.FindPrimaryKey();
            Assert.NotNull(primaryKey);
            Assert.Single(primaryKey.Properties);
            Assert.Equal(nameof(Domain.Entities.Notification.Id), primaryKey.Properties[0].Name);

            // UserId is required
            var userIdProp = entityType.FindProperty(nameof(Domain.Entities.Notification.UserId));
            Assert.NotNull(userIdProp);
            Assert.False(userIdProp.IsNullable);

            // Type is required with max length 128
            var typeProp = entityType.FindProperty(nameof(Domain.Entities.Notification.Type));
            Assert.NotNull(typeProp);
            Assert.False(typeProp.IsNullable);
            Assert.Equal(128, typeProp.GetMaxLength());

            // Title is required with max length 256
            var titleProp = entityType.FindProperty(nameof(Domain.Entities.Notification.Title));
            Assert.NotNull(titleProp);
            Assert.False(titleProp.IsNullable);
            Assert.Equal(256, titleProp.GetMaxLength());

            // Message is optional with max length 2000
            var messageProp = entityType.FindProperty(nameof(Domain.Entities.Notification.Message));
            Assert.NotNull(messageProp);
            Assert.True(messageProp.IsNullable);
            Assert.Equal(2000, messageProp.GetMaxLength());

            // IsRead is required with default value false
            var isReadProp = entityType.FindProperty(nameof(Domain.Entities.Notification.IsRead));
            Assert.NotNull(isReadProp);
            Assert.False(isReadProp.IsNullable);
            Assert.Equal(false, isReadProp.GetDefaultValue());

            // CreatedAt is required
            var createdAtProp = entityType.FindProperty(nameof(Domain.Entities.Notification.CreatedAt));
            Assert.NotNull(createdAtProp);
            Assert.False(createdAtProp.IsNullable);

            // ReadAt is nullable
            var readAtProp = entityType.FindProperty(nameof(Domain.Entities.Notification.ReadAt));
            Assert.NotNull(readAtProp);
            Assert.True(readAtProp.IsNullable);

            // Foreign Key to User
            var userFk = entityType.GetForeignKeys()
                .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(User));
            Assert.NotNull(userFk);
            Assert.Equal(nameof(Domain.Entities.Notification.UserId), userFk.Properties[0].Name);
            Assert.Equal(DeleteBehavior.Cascade, userFk.DeleteBehavior);

            // Foreign Key to Operation (FabricOperation)
            var operationFk = entityType.GetForeignKeys()
                .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(FabricOperation));
            Assert.NotNull(operationFk);
            Assert.Equal(nameof(Domain.Entities.Notification.OperationId), operationFk.Properties[0].Name);
            Assert.Equal(DeleteBehavior.SetNull, operationFk.DeleteBehavior);

            // Composite Index on (UserId, CreatedAt)
            var compositeIndex = entityType.GetIndexes()
                .FirstOrDefault(idx => idx.Properties.Count == 2
                                       && idx.Properties[0].Name == nameof(Domain.Entities.Notification.UserId)
                                       && idx.Properties[1].Name == nameof(Domain.Entities.Notification.CreatedAt));
            Assert.NotNull(compositeIndex);
        }
    }
}
