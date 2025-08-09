using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EF.Core.HumanReadableLog.Extensions;

/// <summary>
/// DbContextOptionsBuilder extensions to attach the audit interceptor.
/// </summary>
public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Adds the <see cref="AuditingSaveChangesInterceptor"/> from the service provider to the builder.
    /// </summary>
    /// <param name="builder">The options builder.</param>
    /// <param name="sp">The service provider to resolve the interceptor from.</param>
    /// <returns>The same builder instance for chaining.</returns>
    public static DbContextOptionsBuilder UseAuditLogging(this DbContextOptionsBuilder builder, IServiceProvider sp)
    {
        var interceptor = sp.GetRequiredService<AuditingSaveChangesInterceptor>();
        builder.AddInterceptors(interceptor);
        return builder;
    }
}
