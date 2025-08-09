namespace EF.Core.HumanReadableLog.Structured.Persistence;

/// <summary>
/// Query API to retrieve structured audit events by root/anchor entity.
/// </summary>
public interface IAuditHistoryReader
{
    /// <summary>
    /// Returns audit events that reference the given root entity, ordered by time.
    /// </summary>
    /// <param name="rootType">The CLR type name of the root entity (e.g., "User").</param>
    /// <param name="rootId">The root entity id (string representation; composite keys are concatenated).</param>
    /// <param name="ct">Cancellation token.</param>
    IAsyncEnumerable<Structured.AuditEvent> GetByRootAsync(string rootType, string rootId, CancellationToken ct = default);

    /// <summary>
    /// Returns audit events by root with optional time range and paging.
    /// </summary>
    /// <param name="rootType">The CLR type name of the root entity.</param>
    /// <param name="rootId">The root id string.</param>
    /// <param name="fromUtc">Inclusive start time (UTC), or null.</param>
    /// <param name="toUtc">Exclusive end time (UTC), or null.</param>
    /// <param name="skip">Number of entries to skip (for paging).</param>
    /// <param name="take">Max number of entries to take (for paging). Use null to take all.</param>
    /// <param name="ct">Cancellation token.</param>
    IAsyncEnumerable<Structured.AuditEvent> GetByRootAsync(string rootType, string rootId, DateTime? fromUtc, DateTime? toUtc, int skip = 0, int? take = null, CancellationToken ct = default);
}
