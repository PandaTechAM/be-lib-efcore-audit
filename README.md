# Pandatech.EFCore.Audit

Automatic audit trail tracking for Entity Framework Core 8+ with configurable property transformations, composite key support, and manual bulk auditing.

## Installation

```bash
dotnet add package Pandatech.EFCore.Audit
```

## Quick Start

### 1. Configure Entities for Auditing

```csharp
public class Blog
{
    public int Id { get; set; }
    public string Title { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] EncryptedKey { get; set; }
}

public class BlogAuditConfiguration : AuditTrailConfigurator<Blog>
{
    public BlogAuditConfiguration()
    {
        SetReadPermission(Permission.UserPermission);
        WriteAuditTrailOnEvents(AuditActionType.Create, AuditActionType.Update, AuditActionType.Delete);
        RuleFor(x => x.EncryptedKey).Transform(Convert.ToBase64String);
    }
}
```

### 2. Create Audit Consumer

```csharp
public class AuditTrailConsumer : IAuditTrailConsumer
{
    public Task ConsumeAuditTrailAsync(AuditTrailEventData eventData, CancellationToken ct = default)
    {
        // Log to database, send to message queue, etc.
        Console.WriteLine(JsonSerializer.Serialize(eventData));
        return Task.CompletedTask;
    }
}
```

### 3. Register Services

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register audit trail BEFORE DbContext
builder.AddAuditTrail<AuditTrailConsumer>(typeof(Program).Assembly);

// Register DbContext with audit interceptors
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseNpgsql(connectionString)
           .AddAuditTrailInterceptors(sp);
});
```

## Configuration API

### Entity Configuration

```csharp
public class PostAuditConfiguration : AuditTrailConfigurator<Post>
{
    public PostAuditConfiguration()
    {
        // Set service name for this entity
        SetServiceName("BlogService");
        
        // Track only specific events (default: all events)
        WriteAuditTrailOnEvents(AuditActionType.Create, AuditActionType.Update);
        
        // Set read permission for row-level security
        SetReadPermission(Permission.AdminOnly);
        
        // Property transformations
        RuleFor(x => x.Content).Ignore();                    // Don't track
        RuleFor(x => x.Title).Rename("PostTitle");          // Rename in audit
        RuleFor(x => x.Price).Transform(x => Math.Round(x, 2)); // Transform value
    }
}
```

### Property Configuration Methods

| Method | Description | Example |
|--------|-------------|---------|
| `Ignore()` | Skip tracking this property | `RuleFor(x => x.Password).Ignore()` |
| `Rename(string)` | Change property name in audit trail | `RuleFor(x => x.Email).Rename("UserEmail")` |
| `Transform<TOutput>(Func)` | Transform value before auditing | `RuleFor(x => x.Data).Transform(Encrypt)` |

### Entity Configuration Methods

| Method | Description |
|--------|-------------|
| `SetServiceName(string)` | Identify the originating service |
| `SetReadPermission(object)` | Set permission level for row-level security |
| `WriteAuditTrailOnEvents(params AuditActionType[])` | Track only specific events (Create, Update, Delete) |

## Audit Event Data

```csharp
public record AuditTrailEventData(List<AuditTrailEventEntity> Entities);

public record AuditTrailEventEntity(
    EntityEntry? EntityEntry,      // EF Core entity entry (null for manual audits)
    string? ServiceName,            // Service identifier
    AuditActionType ActionType,    // Create, Update, or Delete
    string Name,                    // Entity type name
    object? ReadPermission,        // Permission level
    string PrimaryKeyValue,        // Composite keys joined with '_'
    Dictionary<string, object?> TrackedProperty
);
```

**Example audit event:**
```json
{
  "entities": [{
    "serviceName": "BlogService",
    "actionType": "Update",
    "name": "Blog",
    "readPermission": 1,
    "primaryKeyValue": "123",
    "trackedProperty": {
      "title": "New Title",
      "encryptedKey": "AQID"
    }
  }]
}
```

## Manual Bulk Auditing

For operations outside EF Core's change tracker (raw SQL, `ExecuteUpdate`, `AsNoTracking`, external APIs):

```csharp
public class MyService
{
    private readonly IAuditTrailPublisher _publisher;
    
    public async Task BulkCreatePosts()
    {
        // Your untracked operations...
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Posts ...");
        
        // Manually create audit entries
        var auditEntries = new List<ManualAuditEntry>
        {
            new(
                EntityType: typeof(Post),
                Action: AuditActionType.Create,
                ChangedItems: new List<AuditEntryDetail>
                {
                    new(
                        PrimaryKeyIds: ["1"],
                        ChangedProperties: new Dictionary<string, object?>
                        {
                            ["Title"] = "My Post",
                            ["Content"] = "Post content",
                            ["BlogId"] = 42
                        }
                    ),
                    new(
                        PrimaryKeyIds: ["2"],
                        ChangedProperties: new Dictionary<string, object?>
                        {
                            ["Title"] = "Another Post",
                            ["Content"] = "More content",
                            ["BlogId"] = 42
                        }
                    )
                }
            )
        };
        
        await _publisher.BulkAuditAsync(auditEntries);
    }
}
```

## Features

✅ **Automatic change tracking** - Hooks into EF Core's change tracker  
✅ **Composite key support** - Concatenates composite keys with `_` delimiter  
✅ **Property transformations** - Ignore, rename, or transform tracked properties  
✅ **Transaction support** - Publishes audit events after transaction commit  
✅ **Manual bulk auditing** - Track untracked operations like raw SQL  
✅ **Configurable permissions** - Row-level security support via `SetReadPermission`  
✅ **Service identification** - Track which service made the change  
✅ **Partial tracking** - For updates, only changed properties are tracked

## Limitations

⚠️ **Not atomic** - Event-based architecture means audit data could be lost in edge cases  
⚠️ **No untracked operations** - `AsNoTracking`, `ExecuteUpdate`, `ExecuteDelete` are not automatically audited (use manual bulk audit)

## Advanced Usage

### Composite Keys

```csharp
public class OrderItem
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    // ...
}
```

**Audit event:** `primaryKeyValue: "123_456"` (OrderId_ProductId)

### Custom Transformations

```csharp
public class UserAuditConfiguration : AuditTrailConfigurator<User>
{
    public UserAuditConfiguration()
    {
        RuleFor(x => x.Email).Transform(x => MaskEmail(x));
        RuleFor(x => x.Salary).Transform(x => x * 0.01m); // Store in cents
        RuleFor(x => x.CreatedAt).Transform(x => x.ToString("o")); // ISO 8601
    }
    
    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        return parts.Length == 2 
            ? $"{parts[0][0]}***@{parts[1]}" 
            : email;
    }
}
```

### Selective Event Tracking

```csharp
public class SensitiveDataConfig : AuditTrailConfigurator<SensitiveData>
{
    public SensitiveDataConfig()
    {
        // Only track deletions, not creates or updates
        WriteAuditTrailOnEvents(AuditActionType.Delete);
    }
}
```

## Event Consumer Examples

### Database Consumer

```csharp
public class DatabaseAuditConsumer : IAuditTrailConsumer
{
    private readonly AuditDbContext _auditDb;
    
    public async Task ConsumeAuditTrailAsync(AuditTrailEventData eventData, CancellationToken ct)
    {
        var auditRecords = eventData.Entities.Select(e => new AuditRecord
        {
            EntityName = e.Name,
            Action = e.ActionType.ToString(),
            PrimaryKey = e.PrimaryKeyValue,
            Changes = JsonSerializer.Serialize(e.TrackedProperty),
            Timestamp = DateTime.UtcNow,
            ServiceName = e.ServiceName,
            Permission = e.ReadPermission?.ToString()
        });
        
        _auditDb.AuditRecords.AddRange(auditRecords);
        await _auditDb.SaveChangesAsync(ct);
    }
}
```

### Message Queue Consumer

```csharp
public class RabbitMqAuditConsumer : IAuditTrailConsumer
{
    private readonly IMessagePublisher _publisher;
    
    public async Task ConsumeAuditTrailAsync(AuditTrailEventData eventData, CancellationToken ct)
    {
        var message = new AuditMessage
        {
            Timestamp = DateTime.UtcNow,
            Events = eventData.Entities
        };
        
        await _publisher.PublishAsync("audit-trail", message, ct);
    }
}
```

## Registration Order

⚠️ **Important:** `AddAuditTrail` must be called **before** `AddDbContext`:

```csharp
// ✅ Correct order
builder.AddAuditTrail<AuditConsumer>(typeof(Program).Assembly);
builder.Services.AddDbContext<AppDbContext>(...);

// ❌ Wrong order - will not work
builder.Services.AddDbContext<AppDbContext>(...);
builder.AddAuditTrail<AuditConsumer>(typeof(Program).Assembly);
```

If you must register DbContext first, manually add `HttpContextAccessor`:

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<AppDbContext>(...);
builder.AddAuditTrail<AuditConsumer>(typeof(Program).Assembly);
```

## Supported Databases

- PostgreSQL
- SQL Server
- MySQL
- SQLite
- Any database supported by EF Core Relational

## License

MIT