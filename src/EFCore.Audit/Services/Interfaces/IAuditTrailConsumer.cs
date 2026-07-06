using EFCore.Audit.Models;

namespace EFCore.Audit.Services.Interfaces;

/// <summary>Receives audit trail events for persistence or forwarding. Implemented by the consumer.</summary>
public interface IAuditTrailConsumer
{
    /// <summary>Handle the audit trail produced by a completed save or transaction.</summary>
    public Task ConsumeAuditTrailAsync(AuditTrailEventData auditTrailEventData, CancellationToken ct = default);
}
