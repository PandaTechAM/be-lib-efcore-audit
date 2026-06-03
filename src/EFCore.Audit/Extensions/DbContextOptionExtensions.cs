using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.Audit.Extensions;

public static class DbContextOptionExtensions
{
   public static DbContextOptionsBuilder AddAuditTrailInterceptors(this DbContextOptionsBuilder optionsBuilder, IServiceProvider sp)
   {
      optionsBuilder.AddInterceptors(sp.GetRequiredService<SaveChangesAuditorInterceptor>());
      optionsBuilder.AddInterceptors(sp.GetRequiredService<TransactionAuditorInterceptor>());

      return optionsBuilder;
   }
}