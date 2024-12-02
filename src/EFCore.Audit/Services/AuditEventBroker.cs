using EFCore.Audit.Models;

namespace EFCore.Audit.Services;

internal static class AuditEventBroker
{
   public static event Action<AuditTrailEventData>? AuditTrailPublished;

   internal static void Publish(AuditTrailEventData auditEventData)
   {
      AuditTrailPublished?.Invoke(auditEventData);
   }
}
