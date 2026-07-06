namespace EFCore.Audit.Models;

/// <summary>Kind of change captured for an audited entity.</summary>
public enum AuditActionType
{
    /// <summary>Entity was inserted.</summary>
    Create = 1,

    /// <summary>Entity was modified.</summary>
    Update = 2,

    /// <summary>Entity was removed.</summary>
    Delete = 3,

    /// <summary>Change that is outside the audit scope.</summary>
    Other = 4
}
