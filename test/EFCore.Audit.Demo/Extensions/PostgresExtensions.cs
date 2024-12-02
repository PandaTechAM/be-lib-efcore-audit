using EFCore.Audit.Extensions;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Audit.Demo.Extensions;

public static class PostgresExtensions
{
   public static WebApplicationBuilder AddPostgresContext<TContext>(this WebApplicationBuilder builder,
      string connectionString)
      where TContext : DbContext
   {
      builder.Services.AddDbContextPool<TContext>((sp,options) =>
      {
         options
            .UseNpgsql(connectionString)
            .AddAuditTrailInterceptors(sp);
      });


      return builder;
   }


   public static WebApplication EnsureCleanDb<TContext>(this WebApplication app)
      where TContext : DbContext
   {
      using var scope = app.Services.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
      dbContext.Database.EnsureDeleted();
      dbContext.Database.EnsureCreated();
      return app;
   }
}