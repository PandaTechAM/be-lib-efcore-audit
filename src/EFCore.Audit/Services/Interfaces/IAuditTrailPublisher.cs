using EFCore.Audit.Models;

namespace EFCore.Audit.Services.Interfaces;

public interface IAuditTrailPublisher
{
   Task BulkAuditAsync(List<ManualAuditEntry> auditEntries, CancellationToken cancellationToken = default);
}