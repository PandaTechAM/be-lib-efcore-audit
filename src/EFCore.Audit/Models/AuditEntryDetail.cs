namespace EFCore.Audit.Models;

/// <summary>Describes a single changed entity for a manual audit entry.</summary>
/// <param name="PrimaryKeyIds">Primary key component values identifying the entity.</param>
/// <param name="ChangedProperties">Changed property names mapped to their new values.</param>
public record AuditEntryDetail(
    List<string> PrimaryKeyIds,
    Dictionary<string, object?> ChangedProperties);
