using EFCore.Audit.Configurator;

namespace EFCore.Audit.Demo.Entities;

public class Post
{
   public int Id { get; set; }

   public int BlogId { get; set; }
   public required string Title { get; set; }
   public required string Content { get; set; }
   public Blog Blog { get; set; } = null!;
}

public class PostAuditConfiguration : AuditTrailConfigurator<Post>
{
   public PostAuditConfiguration()
   {
      SetServiceName("Monolith");
      RuleFor(s => s.Content).Ignore();
      RuleFor(s => s.Title).Rename("TotallyNewTitle");
   }
}