using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EFCore.Audit.Models;

/// <summary>Snapshot of a tracked entity captured while auditing a save operation.</summary>
public class AuditedEntity
{
    /// <summary>EF Core change-tracking entry for the entity.</summary>
    public required EntityEntry Entry { get; set; }

    /// <summary>CLR type of the entity.</summary>
    public required Type Type { get; set; }

    /// <summary>Change kind for the entity.</summary>
    public AuditActionType ActionType { get; set; }

    /// <summary>Entity type name.</summary>
    public required string Name { get; set; }

    /// <summary>Composite primary key rendered as a string.</summary>
    public required string PrimaryKeyValue { get; set; }

    /// <summary>Property values before the change.</summary>
    public Dictionary<string, object?> PropertyOriginalValues { get; set; } = [];

    /// <summary>Property values after the change.</summary>
    public Dictionary<string, object?> PropertyCurrentValues { get; set; } = [];
}
