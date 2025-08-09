namespace EF.Core.HumanReadableLog.Structured;

internal sealed class PreCollectionDelta
{
    public required string CollectionDisplay { get; init; }
    public required string RelatedEntityType { get; init; }
    public required object? RelatedEntityKeySnapshot { get; init; }
    public required string? RelatedEntityTitle { get; init; }
    public required bool Added { get; init; }
}
