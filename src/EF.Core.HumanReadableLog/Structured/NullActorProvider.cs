namespace EF.Core.HumanReadableLog.Structured;

internal sealed class NullActorProvider : IAuditActorProvider
{
    public string? GetActor() => null;
}
