using EFCore.Audit.Models;

namespace EFCore.Audit.Services;

public static class AuditTrailSubscriber
{
   public static void ConfigureAuditTrailHandler(Action<AuditTrailEventData> handler)
   {
      AuditEventBroker.AuditTrailPublished += handler;
   }
}