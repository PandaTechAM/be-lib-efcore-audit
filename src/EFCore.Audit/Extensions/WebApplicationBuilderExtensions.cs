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
      var auditTrailConfigurator = AuditTrailConfigurationLoader.LoadFromAssemblies(assemblies);

      builder.Services.AddSingleton(auditTrailConfigurator);

      builder.Services.AddHostedService<CleanupHostedService>();
      return builder;
   }
}