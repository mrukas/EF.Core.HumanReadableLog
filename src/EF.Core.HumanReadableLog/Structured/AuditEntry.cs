namespace EF.Core.HumanReadableLog.Structured;

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
