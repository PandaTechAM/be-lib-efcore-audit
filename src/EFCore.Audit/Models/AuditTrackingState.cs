namespace EFCore.Audit.Models;

/// <summary>Per-DbContext accumulator of tracked entities for one save operation.</summary>
public sealed class AuditTrackingState
{
    /// <summary>Entities tracked so far for the current operation.</summary>
    public List<AuditedEntity> Entities { get; } = [];
}
