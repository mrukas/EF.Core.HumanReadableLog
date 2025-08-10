using EF.Core.HumanReadableLog.Attributes;

namespace SampleApp;

[AuditEntityDisplay("Tier", "Tiere")]
[AuditEntityTitleTemplate("{Name}")]
public class Pet
{
    public int Id { get; set; }

    [AuditEntityTitle]
    [AuditDisplay("Name")]
    public string Name { get; set; } = string.Empty;

    [AuditDisplay("Lieblingsessen")]
    public List<Food> FavoriteFoods { get; set; } = new();
}

[AuditEntityDisplay("Essen", "Essen")]
public class Food
{
    public int Id { get; set; }

    [AuditEntityTitle]
    [AuditDisplay("Name")]
    public string Name { get; set; } = string.Empty;

    [AuditDisplay("Kalorien")]
    public int Calories { get; set; }

    public Pet Pet { get; set; } = null!;

    public int PetId { get; set; }
}

[AuditEntityDisplay("Benutzer", "Benutzer")]
public class User
{
    public int Id { get; set; }

    [AuditDisplay("Haustiere")]
    public List<Pet> Pets { get; set; } = new();

    [AuditDisplay("Name")]
    public string Name { get; set; } = string.Empty;
}