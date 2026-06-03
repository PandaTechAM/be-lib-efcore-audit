namespace EFCore.Audit.Models;

public sealed class AuditTrackingState
{
   public List<AuditedEntity> Entities { get; } = [];
}
