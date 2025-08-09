namespace EF.Core.HumanReadableLog.Structured;

/// <summary>
/// Identifies a root/anchor entity for grouping histories (e.g., a specific User).
/// </summary>
public sealed class AuditAnchor
{
    public required string RootType { get; init; }
    public required string RootId { get; init; }
    public string? RootTitle { get; init; }
}
