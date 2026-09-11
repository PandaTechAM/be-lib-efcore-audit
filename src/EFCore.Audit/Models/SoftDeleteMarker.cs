namespace EFCore.Audit.Models;

/// <summary>Identifies an entity that is deleted by setting a property value instead of issuing a DELETE.</summary>
/// <param name="PropertyName">Entity property holding the deleted flag or status.</param>
/// <param name="DeletedValue">Value of that property that means the entity is deleted.</param>
public sealed record SoftDeleteMarker(string PropertyName, object? DeletedValue);
