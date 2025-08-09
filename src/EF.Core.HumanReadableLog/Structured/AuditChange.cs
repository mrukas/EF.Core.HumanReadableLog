namespace EF.Core.HumanReadableLog.Structured;

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
