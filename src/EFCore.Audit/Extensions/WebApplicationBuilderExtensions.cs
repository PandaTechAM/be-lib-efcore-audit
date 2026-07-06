using System.Reflection;
using EFCore.Audit.Configurator;
using EFCore.Audit.Services.Implementations;
using EFCore.Audit.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.Audit.Extensions;

/// <summary>WebApplicationBuilder extensions for registering audit trail services.</summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    ///     Registers audit trail services and the given consumer, discovering entity configurations
    ///     from the supplied assemblies (defaults to the calling assembly when none are given).
    /// </summary>
    public static WebApplicationBuilder AddAuditTrail<TConsumer>(this WebApplicationBuilder builder,
        params Assembly[] assemblies) where TConsumer : class, IAuditTrailConsumer
    {
        if (assemblies.Length == 0)
        {
            assemblies = [Assembly.GetCallingAssembly()];
        }

        var auditTrailConfigurator = AuditTrailConfigurationLoader.LoadFromAssemblies(assemblies);

        builder.Services.AddSingleton(auditTrailConfigurator);
        builder.Services.AddScoped<IAuditTrailPublisher, AuditTrailPublisher>();
        builder.Services.AddScoped<IAuditTrailConsumer, TConsumer>();
        builder.Services.AddSingleton<SaveChangesAuditorInterceptor>();
        builder.Services.AddSingleton<TransactionAuditorInterceptor>();
        builder.Services.AddSingleton<AuditTrailTrackingService>();

        return builder;
    }
}
