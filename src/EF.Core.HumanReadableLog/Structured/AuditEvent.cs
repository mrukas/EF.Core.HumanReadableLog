namespace EF.Core.HumanReadableLog.Structured;

/// <summary>
/// A persisted audit event corresponding to a single SaveChanges call.
/// </summary>
public sealed class AuditEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    public string? Actor { get; init; }
    public string? CorrelationId { get; init; }
    public string? TenantId { get; init; }

    public List<AuditEntry> Entries { get; } = new();
}
