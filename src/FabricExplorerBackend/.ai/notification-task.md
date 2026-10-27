## Notification Feature Specification

Implement the initial Notification feature for the backend.

### 1. Purpose

The Notification feature is responsible for storing user-facing notifications that should appear in the frontend notification/bell UI.

A Notification is **not** the same as an Operation.

- `Operation` is used to track backend/Fabric long-running operations (LROs).
- `Notification` is used to inform the user about an event or result that is relevant to them.

Do not modify or replace the existing `Operations` model.

A notification may optionally reference an existing `Operation`.

---

## 2. Notification Entity

Create a `Notification` entity with the following fields (these are just recommend fields):

```csharp
public class Notification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = null!;

    public string? Message { get; set; }

    public Guid? OperationId { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }
}
```

### Field descriptions

#### `Id`

Unique identifier of the notification.

- Type: `Guid`
- Primary key.

#### `UserId`

Identifies the user who owns the notification.

- Type: `Guid`
- Required.
- A notification must belong to exactly one application user.

#### `Type`

Defines what kind of notification this is.

- Type: `NotificationType`
- Required.

The type should describe the business event rather than simply describing the HTTP request.

#### `Title`

Short user-facing notification title.

Examples:

```text
Load table completed
Load table failed
Connection created
Connection deleted
```

- Type: `string`
- Required.

#### `Message`

Optional additional information displayed to the user.

Examples:

```text
The Customers table was loaded successfully.
Unable to load the Customers table.
```

- Type: `string?`
- Nullable.

#### `OperationId`

Optional reference to an existing `Operation`.

- Type: `Guid?`
- Nullable.

This is required only when the notification is related to an existing backend/Fabric operation.

For example:

```text
Load Table
    ↓
Operation
    ↓
Succeeded
    ↓
Notification
OperationId = <operation id>
```

Notifications that are not related to an LRO should have `OperationId = null`.

Do not create a new Operation only for the purpose of creating a Notification.

#### `IsRead`

Indicates whether the user has read the notification. This can be used at a caculated property instead

- Type: `bool`
- Required.
- Default value: `false`.

#### `CreatedAt`

The timestamp when the notification was created.

- Type: `DateTime`
- Required.

#### `ReadAt`

The timestamp when the user marked the notification as read.

- Type: `DateTime?`
- Nullable.

When `IsRead` is `false`, this should normally be `null`.

When the notification is marked as read, set this value to the current timestamp.

---

## 3. Notification Type

Create a `NotificationType` class.

For the initial implementation:

```csharp
public class NotificationType
{
    public static class Connection
    {
        public const string CreateConnectionSucceeded = "connection.created.succeeded";
        public const string CreateConnectionFailed = "connection.created.failed";
        public const string DeleteConnectionSucceeded = "connection.deleted.succeeded";
        public const string DeleteConnectionFailed = "connection.deleted.failed";
        public const string UpdateConnectionSucceeded = "connection.updated.failed";
        public const string UpdateConnectionFailed = "connection.updated.failed";
        public const string TestConnectionSucceeded = "connection.tested.succeeded";
        public const string TestConnectionFailed = "connection.tested.failed";
    }
}
```

The enum represents the **business event**, not the HTTP method or endpoint.

For example:

```text
POST /api/connections
    ↓
ConnectionCreated
```

and:

```text
Load Table LRO
    ↓
OperationSucceeded
```

or:

```text
Load Table LRO
    ↓
OperationFailed
```

Do not create a generic `Success` / `Failed` notification type if the specific business event can be represented.

---

## 4. Relationship with Operations

The existing `Operations` table is responsible for tracking LROs.

Do not merge `Notification` and `Operation`.

The relationship should be:

```text
User
  |
  | 1
  |
  | *
Notification
  |
  | 0..1
  |
Operation
```

In other words:

- One user can have many notifications.
- A notification can optionally reference one operation.
- An operation does not necessarily need to have a notification.

Example:

```text
Operations
--------------------------------
Id: O123
Status: Succeeded
```

```text
Notifications
--------------------------------
Id: N123
UserId: U123
Type: OperationSucceeded
Title: "Load table completed"
Message: "Customers table loaded successfully."
OperationId: O123
IsRead: false
CreatedAt: ...
ReadAt: null
```

For a non-LRO action:

```text
Notifications
--------------------------------
Id: N124
UserId: U123
Type: ConnectionCreated
Title: "Connection created"
Message: "Customer DB connection was created successfully."
OperationId: null
IsRead: false
CreatedAt: ...
ReadAt: null
```

---

## 5. Important Responsibility Boundary

Do not make Notification responsible for determining whether an operation succeeded or failed.

The existing operation-processing/polling logic determines the final operation status.

For example:

```text
Load Table requested
        ↓
Create/track Operation
        ↓
Operation is Running
        ↓
Poll Fabric
        ↓
Operation Succeeded
        ↓
Create OperationSucceeded Notification
```

Or:

```text
Operation Failed
        ↓
Create OperationFailed Notification
```

The Notification represents the user-facing result; it is not the source of truth for the operation status.

---

## 6. EF Core Configuration

Add the corresponding `DbSet` to the existing `DbContext`:

```csharp
public DbSet<Notification> Notifications { get; set; }
```

Create an EF Core migration for the new table.

The table should have:

- Primary key on `Id`.
- Required foreign key to the application user represented by `UserId`.
- Optional foreign key to `Operations` through `OperationId`, if the existing project structure supports this relationship.
- Appropriate indexes for notification retrieval.

At minimum, create an index optimized for querying a user's latest notifications, for example:

```text
(UserId, CreatedAt)
```

The exact index configuration should follow the existing project's EF Core conventions.

Do not introduce unrelated schema changes.

---

## 7. Read/Unread Behavior

A newly created notification must have:

```text
IsRead = false
ReadAt = null
```

When the user marks it as read:

```text
IsRead = true
ReadAt = current timestamp
```

Do not delete notifications simply because they have been read.

The notification history should remain available unless a separate retention/deletion requirement is introduced later.

---

## 8. Realtime Delivery

The Notification table is the source of truth.

SSE is only the realtime delivery mechanism to the frontend.

The intended architecture is:

```text
Business Action
      ↓
Operation (when applicable)
      ↓
Create Notification
      ↓
Save Notification to database
      ↓
Publish Notification through SSE
      ↓
Frontend updates notification bell
```

The frontend should not continuously poll the Notification endpoint.

The frontend can use:

```text
GET /api/notifications
```

to retrieve notification history and an SSE endpoint such as:

```text
GET /api/notifications/stream
```

for realtime notifications.

The SSE implementation is outside the scope of the initial Notification entity/schema implementation unless explicitly requested.

The database must remain the source of truth because an SSE connection can disconnect and the frontend may miss events.

---

## 9. Scope of This Implementation

For this implementation, focus on:

1. `Notification` entity.
2. `NotificationType` enum.
3. EF Core `DbSet`.
4. EF Core relationship/configuration.
5. Database migration.
6. Appropriate indexes.
7. Follow the existing project's conventions for entities, namespaces, IDs, timestamps, relationships, and configuration.

Do not:

- Replace or modify the existing `Operations` system.
- Create an Operation just to represent a Notification.
- Create notifications for every API request automatically.
- Add polling logic.
- Add SSE infrastructure unless explicitly requested.
- Add unnecessary fields such as `Priority`, `ExpiresAt`, `Metadata`, `EntityType`, or `EntityId`.
- Introduce a generic notification framework or event bus unless the existing architecture already requires it.

The goal is to establish a clean and minimal Notification persistence model that can later be consumed by REST APIs and SSE.