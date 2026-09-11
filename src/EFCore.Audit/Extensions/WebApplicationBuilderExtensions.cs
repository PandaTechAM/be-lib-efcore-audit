using System.Reflection;
using System.Runtime.CompilerServices;
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
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static WebApplicationBuilder AddAuditTrail<TConsumer>(this WebApplicationBuilder builder,
        params Assembly[] assemblies) where TConsumer : class, IAuditTrailConsumer
    {
        return builder.AddAuditTrailCore<TConsumer>(new AuditTrailOptions(),
            assemblies.Length == 0 ? [Assembly.GetCallingAssembly()] : assemblies);
    }

    /// <summary>
    ///     Registers audit trail services and the given consumer with registration-wide options, discovering entity
    ///     configurations from the supplied assemblies (defaults to the calling assembly when none are given).
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static WebApplicationBuilder AddAuditTrail<TConsumer>(this WebApplicationBuilder builder,
        Action<AuditTrailOptions> configure,
        params Assembly[] assemblies) where TConsumer : class, IAuditTrailConsumer
    {
        var options = new AuditTrailOptions();
        configure(options);

        return builder.AddAuditTrailCore<TConsumer>(options,
            assemblies.Length == 0 ? [Assembly.GetCallingAssembly()] : assemblies);
    }

    private static WebApplicationBuilder AddAuditTrailCore<TConsumer>(this WebApplicationBuilder builder,
        AuditTrailOptions options,
        Assembly[] assemblies) where TConsumer : class, IAuditTrailConsumer
    {
        var auditTrailConfigurator = AuditTrailConfigurationLoader.LoadFromAssemblies(options, assemblies);

        builder.Services.AddSingleton(auditTrailConfigurator);
        builder.Services.AddScoped<IAuditTrailPublisher, AuditTrailPublisher>();
        builder.Services.AddScoped<IAuditTrailConsumer, TConsumer>();
        builder.Services.AddSingleton<SaveChangesAuditorInterceptor>();
        builder.Services.AddSingleton<TransactionAuditorInterceptor>();
        builder.Services.AddSingleton<AuditTrailTrackingService>();

        return builder;
    }
}
