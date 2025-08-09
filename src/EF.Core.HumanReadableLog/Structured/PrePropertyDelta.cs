namespace EF.Core.HumanReadableLog.Structured;

internal sealed class PrePropertyDelta
{
    public required string PropertyName { get; init; }
    public required string DisplayName { get; init; }
    public object? Original { get; init; }
    public object? Current { get; init; }
}
