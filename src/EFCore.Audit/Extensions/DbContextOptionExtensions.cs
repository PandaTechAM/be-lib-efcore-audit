using EFCore.Audit.Configurator;
using EFCore.Audit.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.Audit.Extensions;

public static class DbContextOptionExtensions
{
   public static DbContextOptionsBuilder AddAuditTrailInterceptors(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider)
   {
      var auditTrailConfigurator = serviceProvider.GetRequiredService<AuditTrailConfigurator>();
      optionsBuilder.AddInterceptors(new SaveChangesAuditorInterceptor(auditTrailConfigurator));
      optionsBuilder.AddInterceptors(new TransactionAuditorInterceptor(auditTrailConfigurator));
      return optionsBuilder;
   }
}