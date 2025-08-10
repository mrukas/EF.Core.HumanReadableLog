using System.Collections.Generic;
using EF.Core.HumanReadableLog.Attributes;

namespace EF.Core.HumanReadableLog.Tests;

[AuditEntityDisplay("Company", "Companies")]
[AuditEntityTitleTemplate("{Name}")]
public class Company
{
    public int Id { get; set; }
    [AuditEntityTitle]
    [AuditDisplay("Name")]
    public string Name { get; set; } = string.Empty;

    [AuditDisplay("Projects")]
    public List<Project> Projects { get; set; } = new();
}

[AuditEntityDisplay("Project", "Projects")]
[AuditEntityTitleTemplate("{Name}")]
public class Project
{
    public int Id { get; set; }
    [AuditEntityTitle]
    [AuditDisplay("Name")]
    public string Name { get; set; } = string.Empty;

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    [AuditDisplay("Reports")]
    public List<StatusReport> Reports { get; set; } = new();
}

[AuditEntityDisplay("StatusReport", "StatusReports")]
[AuditEntityTitleTemplate("{Title}")]
public class StatusReport
{
    public int Id { get; set; }
    [AuditDisplay("Title")]
    public string Title { get; set; } = string.Empty;

    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    [AuditDisplay("Comments")]
    public List<Comment> Comments { get; set; } = new();
}

[AuditEntityDisplay("Comment", "Comments")]
[AuditEntityTitleTemplate("{Title}")]
public class Comment
{
    public int Id { get; set; }
    [AuditDisplay("Title")]
    public string Title { get; set; } = string.Empty;
    [AuditDisplay("Text")]
    public string Text { get; set; } = string.Empty;

    public int StatusReportId { get; set; }
    public StatusReport? StatusReport { get; set; }
}
