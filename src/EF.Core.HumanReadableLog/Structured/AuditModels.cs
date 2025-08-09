using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EF.Core.HumanReadableLog.Structured;

/// <summary>
/// Type of change recorded.
/// </summary>
public enum AuditChangeType
{
    Property = 0,
    CollectionAdded = 1,
    CollectionRemoved = 2,
    Deleted = 3
}

/// <summary>
/// Identifies a root/anchor entity for grouping histories (e.g., a specific User).
/// </summary>
public sealed class AuditAnchor
{
    public required string RootType { get; init; }
    public required string RootId { get; init; }
    public string? RootTitle { get; init; }
}

/// <summary>
/// A single change within an entry.
/// </summary>
public sealed class AuditChange
{
    public AuditChangeType ChangeType { get; init; }

    // Property change
    public string? PropertyPath { get; init; }
    public string? DisplayName { get; init; }
    public string? Old { get; init; }
    public string? New { get; init; }

    // Collection change
    public string? CollectionDisplay { get; init; }
    public string? RelatedEntityType { get; init; }
    public string? RelatedEntityId { get; init; }
    public string? RelatedEntityTitle { get; init; }

    // Rendered human message (optional but useful for UI)
    public string? Message { get; init; }
}

/// <summary>
/// A set of changes for a single entity (and anchor metadata) within one SaveChanges.
/// </summary>
public sealed class AuditEntry
{
    public required string EntityType { get; init; }
    public required string EntityId { get; init; }
    public string? EntityTitle { get; init; }

    // Anchor info for history queries
    public required string RootType { get; init; }
    public required string RootId { get; init; }
    public string? RootTitle { get; init; }

    public List<AuditChange> Changes { get; } = new();
}

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
