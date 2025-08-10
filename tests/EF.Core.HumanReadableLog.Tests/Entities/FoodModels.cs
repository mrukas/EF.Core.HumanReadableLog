using System.Collections.Generic;
using EF.Core.HumanReadableLog.Attributes;

namespace EF.Core.HumanReadableLog.Tests;

[AuditEntityDisplay("Food", "Foods")]
[AuditEntityTitleTemplate("{Name}")]
public class Food2
{
    public int Id { get; set; }
    [AuditEntityTitle]
    [AuditDisplay("Name")]
    public string Name { get; set; } = string.Empty;
    [AuditDisplay("Calories")]
    public int Calories { get; set; }
}

[AuditEntityDisplay("Pet", "Pets")]
public class Pet2
{
    public int Id { get; set; }
    [AuditDisplay("FavoriteFoods")]
    public List<Food2> FavoriteFoods { get; set; } = new();
}
