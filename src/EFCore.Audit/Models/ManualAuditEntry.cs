namespace EFCore.Audit.Models;

/// <summary>A manually supplied audit record for bulk auditing outside change tracking.</summary>
/// <param name="EntityType">CLR type of the audited entity.</param>
/// <param name="Action">Change kind being recorded.</param>
/// <param name="ChangedItems">Per-entity change details.</param>
public record ManualAuditEntry(
    Type EntityType,
    AuditActionType Action,
    List<AuditEntryDetail> ChangedItems);
