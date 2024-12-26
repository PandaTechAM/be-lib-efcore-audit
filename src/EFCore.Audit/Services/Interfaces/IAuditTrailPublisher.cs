using EFCore.Audit.Models;
using EFCore.Audit.Services.Implementations;

namespace EFCore.Audit.Services.Interfaces;

public interface IAuditTrailPublisher
{
   Task BulkAuditAsync(List<ManualAuditEntry> auditEntries);
}