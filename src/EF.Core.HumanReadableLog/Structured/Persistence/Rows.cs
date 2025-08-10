namespace EF.Core.HumanReadableLog.Structured.Persistence;

internal class AuditEventRow
{
    public Guid Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string? Actor { get; set; }
    public string? CorrelationId { get; set; }
    public string? TenantId { get; set; }
    public List<AuditEntryRow> Entries { get; set; } = new();
}

internal class AuditEntryRow
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public AuditEventRow? Event { get; set; }

    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? EntityTitle { get; set; }

    public string RootType { get; set; } = string.Empty;
    public string RootId { get; set; } = string.Empty;
    public string? RootTitle { get; set; }
    public DateTime TimestampUtc { get; set; }

    public List<AuditChangeRow> Changes { get; set; } = new();
}

internal class AuditChangeRow
{
    public Guid Id { get; set; }
    public Guid EntryId { get; set; }
    public AuditEntryRow? Entry { get; set; }

    public int ChangeType { get; set; }
    public string? PropertyPath { get; set; }
    public string? DisplayName { get; set; }
    public string? Old { get; set; }
    public string? New { get; set; }
    public string? CollectionDisplay { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? RelatedEntityId { get; set; }
    public string? RelatedEntityTitle { get; set; }
    public string? ParentEntityType { get; set; }
    public string? ParentEntityId { get; set; }
    public string? ParentEntityTitle { get; set; }
    public string? Message { get; set; }
}
