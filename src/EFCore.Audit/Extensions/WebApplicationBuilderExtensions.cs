using System.Reflection;
using EFCore.Audit.Configurator;
using EFCore.Audit.Services;
using EFCore.Audit.Services.Implementations;
using EFCore.Audit.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.Audit.Extensions;

public static class WebApplicationBuilderExtensions
{
   public static WebApplicationBuilder AddAuditTrail<TConsumer>(this WebApplicationBuilder builder,
      params Assembly[] assemblies) where TConsumer : class, IAuditTrailConsumer
   {
      if (assemblies.Length == 0)
      {
         assemblies = [Assembly.GetCallingAssembly()];
      }

      var auditTrailConfigurator = AuditTrailConfigurationLoader.LoadFromAssemblies(assemblies);

      builder.Services.AddHttpContextAccessor();
      builder.Services.AddSingleton(auditTrailConfigurator);
      builder.Services.AddScoped<IAuditTrailPublisher, AuditTrailPublisher>();
      builder.Services.AddScoped<AuditTrailTrackingService>();
      builder.Services.AddScoped<IAuditTrailConsumer, TConsumer>();
      return builder;
   }
}