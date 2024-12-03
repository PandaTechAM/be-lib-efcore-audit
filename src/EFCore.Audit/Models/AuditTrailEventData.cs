using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EFCore.Audit.Models;

public record AuditTrailEventData(List<AuditTrailEventEntity> Entities);

public record AuditTrailEventEntity(
   EntityEntry EntityEntry,
   string? ServiceName,
   AuditActionType ActionType,
   string Name,
   object? ReadPermission,
   string PrimaryKeyValue,
   Dictionary<string, object?> TrackedProperty);