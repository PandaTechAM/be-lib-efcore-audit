using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EFCore.Audit.Models;
internal class AuditedEntity
{
   public required EntityEntry Entry { get; set; }
   public required Type Type { get; set; }
   public AuditActionType ActionType { get; set; }
   public required string Name { get; set; }
   public required string PrimaryKeyValue { get; set; }
   public Dictionary<string, object?> PropertyOriginalValues { get; set; } = [];
   public Dictionary<string, object?> PropertyCurrentValues { get; set; } = [];
}