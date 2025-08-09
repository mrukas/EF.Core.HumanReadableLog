using EF.Core.HumanReadableLog.Attributes;

namespace SampleApp;

[AuditEntityDisplay("Haustier", "Haustiere")]
[AuditEntityTitleTemplate("{Name}")]
public class Pet
{
    public int Id { get; set; }

    [AuditEntityTitle]
    [AuditDisplay("Name")]
    public string Name { get; set; } = string.Empty;
}

[AuditEntityDisplay("Benutzer", "Benutzer")]
public class User
{
    public int Id { get; set; }

    [AuditDisplay("Haustiere")]
    public List<Pet> Pets { get; set; } = new();
}
