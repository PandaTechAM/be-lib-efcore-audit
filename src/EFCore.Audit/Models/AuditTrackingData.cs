using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EFCore.Audit.Models;

internal class AuditTrackingData
{
   public List<AuditedEntity> Entities { get; set; } = [];
   public DateTime TrackedAt { get; set; } = DateTime.UtcNow;
}

internal class AuditedEntity
{
   public required EntityEntry Entry { get; set; }
   public required Type Type { get; set; }
   public AuditActionType ActionType { get; set; }
   public required string Name { get; set; }
   public required string PrimaryKeyValue { get; set; }
   public object? ReadPermission { get; set; }
   public Dictionary<string, object?> TrackedProperty { get; set; } = [];
}

public enum AuditActionType
{
   Create = 1,
   Update = 2,
   Delete = 3,
   Other = 4
}