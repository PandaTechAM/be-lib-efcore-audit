namespace EFCore.Audit.Models;

public class PropertyAuditConfiguration
{
   public bool Ignore { get; set; }
   public Func<object?, object?>? Transform { get; set; }
   public string? Name { get; set; }
}