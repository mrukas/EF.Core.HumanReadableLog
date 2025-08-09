namespace EF.Core.HumanReadableLog.Structured;

internal sealed class DefaultCorrelationIdProvider : ICorrelationIdProvider
{
    public string? GetCorrelationId() => System.Diagnostics.Activity.Current?.Id;
}
