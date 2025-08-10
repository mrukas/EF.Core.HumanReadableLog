using System.Collections.Generic;
using EF.Core.HumanReadableLog.Attributes;

namespace EF.Core.HumanReadableLog.Tests;

[AuditEntityDisplay("Pet", "Pets")]
[AuditEntityTitleTemplate("{Name}")]
public class Pet
{
    public int Id { get; set; }
    [AuditEntityTitle]
    [AuditDisplay("Name")]
    public string Name { get; set; } = string.Empty;
}

[AuditEntityDisplay("User", "Users")]
public class User
{
    public int Id { get; set; }
    [AuditDisplay("Pets")]
    public List<Pet> Pets { get; set; } = new();
    [AuditDisplay("Name")]
    public string DisplayName { get; set; } = string.Empty;
    [AuditIgnore]
    public string? InternalCode { get; set; }
}
