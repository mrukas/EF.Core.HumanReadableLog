using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EF.Core.HumanReadableLog.Structured;

/// <summary>
/// Target for receiving structured audit events for persistence.
/// </summary>
public interface IStructuredAuditEventSink
{
    /// <summary>
    /// Writes one or more structured audit events.
    /// </summary>
    Task WriteAsync(IEnumerable<AuditEvent> events, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides the user/actor that initiated SaveChanges.
/// </summary>
public interface IAuditActorProvider
{
    /// <summary>
    /// Returns the current actor identifier (e.g., user id or service principal).
    /// </summary>
    string? GetActor();
}

/// <summary>
/// Provides a correlation id for grouping logs per request/operation.
/// </summary>
public interface ICorrelationIdProvider
{
    /// <summary>
    /// Returns a correlation id for the current operation (e.g., request id or trace id).
    /// </summary>
    string? GetCorrelationId();
}

/// <summary>
/// Optional multi-tenant support.
/// </summary>
public interface ITenantProvider
{
    /// <summary>
    /// Returns the current tenant id, if any.
    /// </summary>
    string? GetTenantId();
}

/// <summary>
/// Resolves one or more anchors (root entities) for a changed entry.
/// </summary>
public interface IAuditRootResolver
{
    /// <summary>
    /// Produces one or more anchors used to group the change under a base entity (e.g., a specific User).
    /// </summary>
    IEnumerable<AuditAnchor> ResolveAnchors(EntityEntry entry);
}

/// <summary>
/// Buffers structured audit data between SavingChanges and SavedChanges.
/// </summary>
public interface IAuditBuffer
{
    /// <summary>
    /// Adds an event to the in-scope buffer.
    /// </summary>
    void Add(AuditEvent evt);
    /// <summary>
    /// Returns the current buffered events without clearing.
    /// </summary>
    IReadOnlyList<AuditEvent> Drain();
    /// <summary>
    /// Clears the buffer.
    /// </summary>
    void Clear();
}
