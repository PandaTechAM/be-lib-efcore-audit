using EFCore.Audit.Models;

namespace EFCore.Audit.Services.Interfaces;

public interface IAuditTrailConsumer
{
   public Task ConsumeAuditTrailAsync(AuditTrailEventData auditTrailEventData, CancellationToken cancellationToken = default);
}