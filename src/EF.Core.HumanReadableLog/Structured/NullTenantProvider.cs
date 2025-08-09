namespace EF.Core.HumanReadableLog.Structured;

internal sealed class NullTenantProvider : ITenantProvider
{
    public string? GetTenantId() => null;
}
