namespace FabricExplorerBackend.Features.Notification.Contracts
{
    public class NotificationType
    {
        public static class Connection
        {
            public const string CreateConnectionSucceeded = "connection.created.succeeded";
            public const string CreateConnectionFailed = "connection.created.failed";
            public const string DeleteConnectionSucceeded = "connection.deleted.succeeded";
            public const string DeleteConnectionFailed = "connection.deleted.failed";
            public const string UpdateConnectionSucceeded = "connection.updated.succeeded";
            public const string UpdateConnectionFailed = "connection.updated.failed";
            public const string TestConnectionSucceeded = "connection.tested.succeeded";
            public const string TestConnectionFailed = "connection.tested.failed";
        }

        public static class Operation
        {
            public const string OperationSucceeded = "operation.succeeded";
            public const string OperationFailed = "operation.failed";
        }
    }
}
