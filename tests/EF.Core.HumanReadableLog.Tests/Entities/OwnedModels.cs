using EF.Core.HumanReadableLog.Attributes;

namespace EF.Core.HumanReadableLog.Tests;

public class OwnedOwner
{
    public int Id { get; set; }
    public Address Address { get; set; } = new();
}

public class Address
{
    [AuditDisplay("Street")]
    public string Street { get; set; } = string.Empty;
    [AuditDisplay("ZIP")]
    public string Zip { get; set; } = string.Empty;
}
