using FabricExplorerBackend.Features.Operation;

namespace FabricExplorerBackend.Features.Notification.Contracts
{
    /// <summary>Catalog of the notifications the application can raise. Add new business events here.</summary>
    public static class NotificationTemplates
    {
        public static class Connection
        {
            public static readonly NotificationTemplate Created = new(
                NotificationType.Connection.CreateConnectionSucceeded, NotificationType.Connection.CreateConnectionFailed,
                "Connection created", "Create connection failed",
                "Connection '{0}' was created successfully.", "Unable to create connection '{0}'.");

            public static readonly NotificationTemplate Updated = new(
                NotificationType.Connection.UpdateConnectionSucceeded, NotificationType.Connection.UpdateConnectionFailed,
                "Connection updated", "Update connection failed",
                "Connection '{0}' was updated successfully.", "Unable to update connection '{0}'.");

            public static readonly NotificationTemplate Deleted = new(
                NotificationType.Connection.DeleteConnectionSucceeded, NotificationType.Connection.DeleteConnectionFailed,
                "Connection deleted", "Delete connection failed",
                "Connection '{0}' was deleted successfully.", "Unable to delete connection '{0}'.");

            public static readonly NotificationTemplate Tested = new(
                NotificationType.Connection.TestConnectionSucceeded, NotificationType.Connection.TestConnectionFailed,
                "Connection test succeeded", "Connection test failed",
                "Connection '{0}' was tested successfully.", "Unable to test connection '{0}'.");
        }

        public static class Operation
        {
            public static readonly NotificationTemplate LoadTable = new(
                NotificationType.Operation.OperationSucceeded, NotificationType.Operation.OperationFailed,
                "Load table completed", "Load table failed",
                "The {0} table was loaded successfully.", "Unable to load the {0} table.");

            public static readonly NotificationTemplate StartMirroring = new(
                NotificationType.Operation.OperationSucceeded, NotificationType.Operation.OperationFailed,
                "Start mirroring completed", "Start mirroring failed",
                "Mirroring was started successfully.", "Unable to start mirroring.");

            public static readonly NotificationTemplate StopMirroring = new(
                NotificationType.Operation.OperationSucceeded, NotificationType.Operation.OperationFailed,
                "Stop mirroring completed", "Stop mirroring failed",
                "Mirroring was stopped successfully.", "Unable to stop mirroring.");

            public static readonly NotificationTemplate Generic = new(
                NotificationType.Operation.OperationSucceeded, NotificationType.Operation.OperationFailed,
                "Operation completed", "Operation failed",
                "The operation completed successfully.", "The operation failed.");

            public static NotificationTemplate For(string operationType) => operationType switch
            {
                FabricOperationTypes.LoadTable => LoadTable,
                FabricOperationTypes.StartMirroring => StartMirroring,
                FabricOperationTypes.StopMirroring => StopMirroring,
                _ => Generic
            };
        }
    }
}
