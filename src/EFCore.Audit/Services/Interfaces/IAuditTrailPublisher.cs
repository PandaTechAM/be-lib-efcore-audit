using EFCore.Audit.Models;

namespace EFCore.Audit.Services.Interfaces;

/// <summary>Publishes manually supplied audit entries outside of EF Core change tracking.</summary>
public interface IAuditTrailPublisher
{
    /// <summary>Audit a batch of manual entries and dispatch them to the consumer.</summary>
    Task BulkAuditAsync(List<ManualAuditEntry> auditEntries, CancellationToken ct = default);
}
