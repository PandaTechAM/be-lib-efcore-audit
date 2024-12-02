namespace EFCore.Audit.Models;

public class EntityAuditConfiguration
{
   public string? ServiceName { get; set; }
   public AuditActionType[]? AuditActions { get; set; }
   public object? PermissionToRead { get; set; }
   public Dictionary<string, PropertyAuditConfiguration> Properties { get; } = new();
}