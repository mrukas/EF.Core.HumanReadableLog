using EF.Core.HumanReadableLog.Sinks;
using EF.Core.HumanReadableLog.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EF.Core.HumanReadableLog.Structured;
using EF.Core.HumanReadableLog.Structured.Persistence;

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
        // Structured defaults
        services.AddScoped<IAuditBuffer, InMemoryAuditBuffer>();
        services.AddScoped<IAuditRootResolver, DefaultAuditRootResolver>();
        services.AddScoped<IAuditActorProvider, NullActorProvider>();
        services.AddScoped<ICorrelationIdProvider, DefaultCorrelationIdProvider>();
        services.AddScoped<ITenantProvider, NullTenantProvider>();
        // Note: IStructuredAuditEventSink is optional. Register EfCoreStructuredAuditSink with AddAuditStore.

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
        services.AddScoped<IAuditBuffer, InMemoryAuditBuffer>();
        services.AddScoped<IAuditRootResolver, DefaultAuditRootResolver>();
        services.AddScoped<IAuditActorProvider, NullActorProvider>();
        services.AddScoped<ICorrelationIdProvider, DefaultCorrelationIdProvider>();
        services.AddScoped<ITenantProvider, NullTenantProvider>();
        services.AddScoped<AuditingSaveChangesInterceptor>();
        return services;
    }

    /// <summary>
    /// Adds an EF Core-based audit store for persistence and history queries.
    /// Call this to persist audit logs; otherwise, only the logger sink is used.
    /// </summary>
    public static IServiceCollection AddEfCoreAuditStore(this IServiceCollection services, Action<DbContextOptionsBuilder>? configure)
    {
        services.AddDbContext<AuditStoreDbContext>(configure ?? (_ => { }));
        services.AddScoped<IStructuredAuditEventSink, EfCoreStructuredAuditSink>();
        services.AddScoped<IAuditHistoryReader, EfCoreAuditHistoryReader>();
        return services;
    }

    /// <summary>
    /// Registers a claims-based actor provider using a delegate for resolving the ClaimsPrincipal (e.g., from IHttpContextAccessor).
    /// </summary>
    public static IServiceCollection AddAuditActorFromClaims(this IServiceCollection services, Func<System.Security.Claims.ClaimsPrincipal?> principalAccessor)
    {
        services.AddSingleton<IAuditActorProvider>(sp => new ClaimsPrincipalActorProvider(principalAccessor));
        return services;
    }
}
