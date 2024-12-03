# Pandatech.EFCore.Audit

`Pandatech.EFCore.Audit` is a powerful and configurable library designed to collect audit trail data from the EF Core
`DbContext` change tracker. It is built with scalability and professional-grade extensibility in mind.

## Features

- **Scalable & Configurable**: Tailor the behavior to meet your project's needs.
- **Composite Key Handling**: Returns concatenated composite keys in a single property using `_` as the delimiter.
- **Property Transformation**: Customize tracked properties (e.g., rename, transform, or ignore).

## Limitations

- Not atomic: Being event-based, there is a risk of losing audit data in edge cases.
- Does not work with untracked operations like `AsNoTracking`, `ExecuteUpdate`, `ExecuteDelete`, etc.

## Installation

Install the NuGet package:

```bash
dotnet add package Pandatech.EFCore.Audit
```

## Integration

To integrate `Pandatech.EFCore.Audit` into your project, follow these steps:

### 1. Configure DbContext

Set up your `DbContext` to include your entities:

```csharp
public class PostgresContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }
}
```

### 2. Configure Entities

Entities can be set up for auditing using custom configurations. Below are examples:

#### Blog Entity

```csharp
public class Blog
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public BlogType BlogType { get; set; }
    public required byte[] EncryptedKey { get; set; }
}

public class BlogAuditTrailConfiguration : AuditTrailConfigurator<Blog>
{
    public BlogAuditTrailConfiguration()
    {
        SetReadPermission(Permission.UserPermission);
        WriteAuditTrailOnEvents(AuditActionType.Create, AuditActionType.Update, AuditActionType.Delete);
        RuleFor(s => s.EncryptedKey).Transform(Convert.ToBase64String);
    }
}

public enum Permission
{
    AdminPermission,
    UserPermission
}
```

#### Post Entity

```csharp
public class Post
{
    public int Id { get; set; }
    public int BlogId { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public Blog Blog { get; set; } = null!;
}

public class PostAuditConfiguration : AuditTrailConfigurator<Post>
{
    public PostAuditConfiguration()
    {
        SetServiceName("BlogService");
        RuleFor(s => s.Content).Ignore();
        RuleFor(s => s.Title).Rename("TotallyNewTitle");
    }
}
```

#### Configuration Details

- **SetServiceName:** Specifies a custom service name that will be returned during the audit trail event. This can be
  useful for identifying the origin of the change.
- **SetReadPermission:** Assigns a predefined permission level included in the event, enabling better control over who
  can access the audit information as row-level security.
- **WriteAuditTrailOnEvents:** Defines the specific events (`Create`, `Update`, `Delete`) on which an entity should be
  tracked. If this option is not configured, all events for the entity **will be tracked by default**.
- **Exclusion of Configuration:** If an entity should not be audited, its configuration should be omitted entirely.
  Entities
  without configuration will not be tracked.
- **Transform:** Allows you to apply a custom function to modify the value of a property before it is recorded in the
  audit trail. For example, this can be used to encrypt/decrypt or format data.
- **Ignore:** Skips tracking of the specified property within the entity. Useful for sensitive or irrelevant data.
- **Rename:** Changes the property name in the audit trail output. This is useful for aligning property names with
  business-specific terminology or conventions.

### 3. Register `DbContext` in `Program.cs`

Register your `DbContext` and add the audit trail interceptors:

```csharp
public static WebApplicationBuilder AddPostgresContext<TContext>(this WebApplicationBuilder builder,
    string connectionString)
    where TContext : DbContext
{
    builder.Services.AddDbContextPool<TContext>((sp, options) =>
    {
        options
            .UseNpgsql(connectionString)
            .AddAuditTrailInterceptors(sp);
    });

    return builder;
}
```

### 4. Set Up the Audit Trail Consumer

To handle audit trail events, create a consumer class that inherits from `IAuditTrailConsumer` and implements the
`ConsumeAuditTrailAsync` method. Implement this method to process audit trail events according to your application's
requirements — for example, logging the events, sending them to an external service, or storing them in a database.

Here is an example implementation that serializes the event data to JSON and writes it to the console:

```csharp
public class AuditTrailConsumer : IAuditTrailConsumer
{
   public Task ConsumeAuditTrailAsync(AuditTrailEventData auditTrailEventData)
   {
      Console.WriteLine(JsonSerializer.Serialize(auditTrailEventData));
      return Task.CompletedTask;
   }
}
```

### 5. `Program.cs` Registration and Configuration

Below is a simplified example of how your `Program.cs` file might look:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register audit trail configurations with the consumer class before adding the DbContext
builder.Services.AddAuditTrail<AuditTrailConsumer>(typeof(Program).Assembly);

// Register DbContext with audit trail interceptors
builder.AddPostgresContext<PostgresContext>(
    "Server=localhost;Port=5432;Database=audit_test;User Id=test;Password=test;Pooling=true;");

var app = builder.Build();

app.Run();
```

> **Note:** The `AddAuditTrail` method registers the audit trail configurations and `HttpContextAccessor` so it should
> be always **before** the `AddDbContext` method. In case of using `AddDbContext` before registration make sure to
> register the `HttpContextAccessor` manually by using `builder.Services.AddHttpContextAccessor()` method in
`Program.cs`.

### 6. Audit Trail Event Data

The audit trail event data is represented by the following classes:

```csharp
public record AuditTrailEventData(List<AuditTrailEventEntity> Entities);

public record AuditTrailEventEntity(
    EntityEntry Entry,
    string? ServiceName,
    AuditActionType ActionType,
    string EntityName,
    object? ReadPermission,
    string PrimaryKeyValue,
    Dictionary<string, object?> TrackedProperties);
```

- AuditTrailEventData: Contains a list of `AuditTrailEventEntity` objects.
- AuditTrailEventEntity: Represents an audited entity with its associated data.
    - Entry: The `EntityEntry` object containing the entity data from `DbContext`.
    - ServiceName: The name of the service where the change originated. Configured manually using `SetServiceName`.
    - ActionType: The type of action performed (`Create`, `Update`, `Delete`).
    - EntityName: The name of the entity.
    - ReadPermission: The assigned permission level for accessing this audit trail. Configured manually using
      `SetReadPermission`.
    - PrimaryKeyValue: The primary key value(s) of the entity.
    - TrackedProperties: A dictionary containing the tracked properties and their values.

## Notes

- **Partial Property Tracking:** For `Update` actions, `TrackedProperties` only includes properties that have been
  modified.
- **Event Handling:** The provided `Console.WriteLine` in the demo is a placeholder. You are responsible for
  implementing your own event handling logic.
- **Database Compatibility:** Compatible with PostgreSQL and other relational databases supported by EF Core.
- **Compatible with `.Net 9 +`

## License

This project is licensed under the MIT License.