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

    public List<Food> FavoriteFoods { get; set; } = new();
}

public class Food {
    public int Id { get; set; }

    [AuditEntityTitle]
    [AuditDisplay("Name")]
    public string Name { get; set; } = string.Empty;

    [AuditDisplay("Kalorien")]
    public int Calories { get; set; }
}

[AuditEntityDisplay("Benutzer", "Benutzer")]
public class User
{
    public int Id { get; set; }

    [AuditDisplay("Haustiere")]
    public List<Pet> Pets { get; set; } = new();
}