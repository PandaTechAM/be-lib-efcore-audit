using EFCore.Audit.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Audit.Extensions;

public static class DbContextOptionExtensions
{
   public static DbContextOptionsBuilder AddAuditTrailInterceptors(this DbContextOptionsBuilder optionsBuilder, IServiceProvider sp)
   {
      var httpContextAccessor = sp.GetHttpAccessor();
      optionsBuilder.AddInterceptors(new SaveChangesAuditorInterceptor(httpContextAccessor));   
      optionsBuilder.AddInterceptors(new TransactionAuditorInterceptor(httpContextAccessor));
      return optionsBuilder;
   }
}