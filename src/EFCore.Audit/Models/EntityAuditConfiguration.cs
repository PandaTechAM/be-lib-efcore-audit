namespace EFCore.Audit.Models;

/// <summary>Audit settings for a single entity type.</summary>
public class EntityAuditConfiguration
{
    /// <summary>Service name attributed to changes of this entity.</summary>
    public string? ServiceName { get; set; }

    /// <summary>Action types that trigger an audit; null audits all.</summary>
    public AuditActionType[]? AuditActions { get; set; }

    /// <summary>Row-level read permission required to view entries for this entity.</summary>
    public object? PermissionToRead { get; set; }

    /// <summary>Per-property audit configuration keyed by property name.</summary>
    public Dictionary<string, PropertyAuditConfiguration> Properties { get; } = new();
}
