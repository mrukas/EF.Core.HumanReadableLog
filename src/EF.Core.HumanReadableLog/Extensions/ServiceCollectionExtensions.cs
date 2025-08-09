using EF.Core.HumanReadableLog.Sinks;
using EF.Core.HumanReadableLog.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EF.Core.HumanReadableLog.Extensions;

/// <summary>
/// ServiceCollection extensions to register audit logging components.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers EF Core audit logging components and configures <see cref="AuditOptions"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for <see cref="AuditOptions"/>.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddEfCoreAuditLogging(this IServiceCollection services, Action<AuditOptions>? configure = null)
    {
        var options = new AuditOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IAuditEventSink, LoggerAuditSink>();
        services.AddScoped<AuditingSaveChangesInterceptor>();
        return services;
    }

    /// <summary>
    /// Registers EF Core audit logging with a specific localizer.
    /// </summary>
    public static IServiceCollection AddEfCoreAuditLogging<TLocalizer>(this IServiceCollection services, Action<AuditOptions>? configure = null)
        where TLocalizer : IAuditLocalizer, new()
    {
        var options = new AuditOptions { Localizer = new TLocalizer() };
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.AddSingleton<IAuditEventSink, LoggerAuditSink>();
        services.AddScoped<AuditingSaveChangesInterceptor>();
        return services;
    }
}
