namespace EFCore.Audit.Models;

public record ManualAuditEntry(
   Type EntityType,
   AuditActionType Action,
   List<AuditEntryDetail> ChangedItems);