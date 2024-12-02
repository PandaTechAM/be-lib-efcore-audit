using System.Reflection;
using EFCore.Audit.Configurator;
using EFCore.Audit.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.Audit.Extensions;

public static class WebApplicationBuilderExtensions
{
   public static WebApplicationBuilder AddAuditTrail(this WebApplicationBuilder builder, params Assembly[] assemblies)
   {
      if (assemblies.Length == 0)
      {
         assemblies = [Assembly.GetCallingAssembly()];
      }
      
      var auditTrailConfigurator = AuditTrailConfigurationLoader.LoadFromAssemblies(assemblies);

      builder.Services.AddHttpContextAccessor();
      builder.Services.AddSingleton(auditTrailConfigurator);
      builder.Services.AddScoped<AuditTrailTrackingService>();
      return builder;
   }
}