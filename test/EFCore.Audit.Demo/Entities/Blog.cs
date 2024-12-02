using EFCore.Audit.Configurator;
using EFCore.Audit.Demo.Enums;
using EFCore.Audit.Models;

namespace EFCore.Audit.Demo.Entities;

public class Blog
{
   public int Id { get; set; }
   public required string Title { get; set; }

   public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
   public BlogType BlogType { get; set; }
   public required byte[] EncryptedKey { get; set; }
}

public class BlogAuditTrailConfiguration : AuditTrailConfigurator<Blog>
{
   public BlogAuditTrailConfiguration()
   {
      SetReadPermission(Permission.UserPermission);
      WriteAuditTrailOnEvents(AuditActionType.Create, AuditActionType.Update, AuditActionType.Delete);

      RuleFor(s => s.EncryptedKey).Transform(Convert.ToBase64String);
   }
}

public enum Permission
{
   AdminPermission,
   UserPermission
}