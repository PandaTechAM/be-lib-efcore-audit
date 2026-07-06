namespace EFCore.Audit.Models;

/// <summary>Audit settings for a single entity property.</summary>
public class PropertyAuditConfiguration
{
    /// <summary>When true, the property is excluded from the audit trail.</summary>
    public bool Ignore { get; set; }

    /// <summary>Optional transform applied to the property value before publishing.</summary>
    public Func<object?, object?>? Transform { get; set; }

    /// <summary>Optional name override used in the audit trail.</summary>
    public string? Name { get; set; }
}
