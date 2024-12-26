namespace EFCore.Audit.Models;

public record AuditEntryDetail(
   List<string> PrimaryKeyIds,
   Dictionary<string, object?> ChangedProperties);