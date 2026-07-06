using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EFCore.Audit.Models;

/// <summary>Payload delivered to consumers describing all audited entities for one operation.</summary>
/// <param name="Entities">Audited entities produced by the operation.</param>
public record AuditTrailEventData(List<AuditTrailEventEntity> Entities);

/// <summary>A single audited entity within an <see cref="AuditTrailEventData" />.</summary>
/// <param name="EntityEntry">EF Core entry for the entity, if still available.</param>
/// <param name="ServiceName">Configured service name for the entity, if any.</param>
/// <param name="ActionType">Change kind for the entity.</param>
/// <param name="Name">Entity type name.</param>
/// <param name="ReadPermission">Row-level read permission required to view the entry, if any.</param>
/// <param name="PrimaryKeyValue">Composite primary key rendered as a string.</param>
/// <param name="TrackedProperty">Audited property names mapped to their (possibly transformed) values.</param>
public record AuditTrailEventEntity(
    EntityEntry? EntityEntry,
    string? ServiceName,
    AuditActionType ActionType,
    string Name,
    object? ReadPermission,
    string PrimaryKeyValue,
    Dictionary<string, object?> TrackedProperty);
