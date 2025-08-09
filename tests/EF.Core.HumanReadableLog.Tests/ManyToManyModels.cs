using System.Collections.Generic;
using EF.Core.HumanReadableLog.Attributes;

namespace EF.Core.HumanReadableLog.Tests;

[AuditEntityDisplay("Benutzer", "Benutzer")]
public class M2MUser
{
    public int Id { get; set; }
    [AuditDisplay("Rollen")]
    public ICollection<M2MRole> Roles { get; set; } = new List<M2MRole>();
}

[AuditEntityDisplay("Rolle", "Rollen")]
[AuditEntityTitleTemplate("{Name}")]
public class M2MRole
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<M2MUser> Users { get; set; } = new List<M2MUser>();
}

// Composite key many-to-many
[AuditEntityDisplay("Links", "Links")]
public class M2MLeft
{
    public int L1 { get; set; }
    public int L2 { get; set; }
    [AuditDisplay("Rechte")]
    public ICollection<M2MRight> Rights { get; set; } = new List<M2MRight>();
}

[AuditEntityDisplay("Rechts", "Rechte")]
[AuditEntityTitleTemplate("{Name}")]
public class M2MRight
{
    public int R1 { get; set; }
    public int R2 { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<M2MLeft> Lefts { get; set; } = new List<M2MLeft>();
}

// One-to-one
public class O2OUser
{
    public int Id { get; set; }
    public O2OProfile? Profile { get; set; }
}

public class O2OProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public O2OUser? User { get; set; }
    [AuditDisplay("Biografie")]
    public string Bio { get; set; } = string.Empty;
}
