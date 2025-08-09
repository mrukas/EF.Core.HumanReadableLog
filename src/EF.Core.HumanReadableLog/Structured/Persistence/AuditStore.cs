using Microsoft.EntityFrameworkCore;

namespace EF.Core.HumanReadableLog.Structured.Persistence;

/// <summary>
/// EF Core DbContext used to persist structured audit logs.
/// </summary>
public sealed class AuditStoreDbContext(DbContextOptions<AuditStoreDbContext> options) : DbContext(options)
{
    /// <summary>Audit events.</summary>
    internal DbSet<AuditEventRow> Events => Set<AuditEventRow>();
    /// <summary>Audit entries (per entity and root) belonging to an event.</summary>
    internal DbSet<AuditEntryRow> Entries => Set<AuditEntryRow>();
    /// <summary>Changes within an audit entry.</summary>
    internal DbSet<AuditChangeRow> Changes => Set<AuditChangeRow>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditEventRow>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.TimestampUtc).IsRequired();
        });
        modelBuilder.Entity<AuditEntryRow>(b =>
        {
            b.HasKey(e => e.Id);
            b.HasIndex(e => new { e.RootType, e.RootId, e.TimestampUtc });
            b.HasOne(e => e.Event)
                .WithMany(e => e.Entries)
                .HasForeignKey(e => e.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<AuditChangeRow>(b =>
        {
            b.HasKey(e => e.Id);
            b.HasOne(e => e.Entry)
                .WithMany(e => e.Changes)
                .HasForeignKey(e => e.EntryId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

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
    public string? Message { get; set; }
}

internal sealed class EfCoreStructuredAuditSink(AuditStoreDbContext db) : IStructuredAuditEventSink
{
    public async Task WriteAsync(IEnumerable<Structured.AuditEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var evt in events)
        {
            var evtRow = new AuditEventRow
            {
                Id = evt.Id,
                TimestampUtc = evt.TimestampUtc,
                Actor = evt.Actor,
                CorrelationId = evt.CorrelationId,
                TenantId = evt.TenantId,
            };

            foreach (var entry in evt.Entries)
            {
                var entryRow = new AuditEntryRow
                {
                    Id = Guid.NewGuid(),
                    Event = evtRow,
                    EntityType = entry.EntityType,
                    EntityId = entry.EntityId,
                    EntityTitle = entry.EntityTitle,
                    RootType = entry.RootType,
                    RootId = entry.RootId,
                    RootTitle = entry.RootTitle,
                    TimestampUtc = evt.TimestampUtc,
                };

                foreach (var ch in entry.Changes)
                {
                    entryRow.Changes.Add(new AuditChangeRow
                    {
                        Id = Guid.NewGuid(),
                        Entry = entryRow,
                        ChangeType = (int)ch.ChangeType,
                        PropertyPath = ch.PropertyPath,
                        DisplayName = ch.DisplayName,
                        Old = ch.Old,
                        New = ch.New,
                        CollectionDisplay = ch.CollectionDisplay,
                        RelatedEntityType = ch.RelatedEntityType,
                        RelatedEntityId = ch.RelatedEntityId,
                        RelatedEntityTitle = ch.RelatedEntityTitle,
                        Message = ch.Message
                    });
                }

                evtRow.Entries.Add(entryRow);
            }

            db.Events.Add(evtRow);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Query API to retrieve structured audit events by root/anchor entity.
/// </summary>
public interface IAuditHistoryReader
{
    /// <summary>
    /// Returns audit events that reference the given root entity, ordered by time.
    /// </summary>
    /// <param name="rootType">The CLR type name of the root entity (e.g., "User").</param>
    /// <param name="rootId">The root entity id (string representation; composite keys are concatenated).</param>
    /// <param name="ct">Cancellation token.</param>
    IAsyncEnumerable<Structured.AuditEvent> GetByRootAsync(string rootType, string rootId, CancellationToken ct = default);

    /// <summary>
    /// Returns audit events by root with optional time range and paging.
    /// </summary>
    /// <param name="rootType">The CLR type name of the root entity.</param>
    /// <param name="rootId">The root id string.</param>
    /// <param name="fromUtc">Inclusive start time (UTC), or null.</param>
    /// <param name="toUtc">Exclusive end time (UTC), or null.</param>
    /// <param name="skip">Number of entries to skip (for paging).</param>
    /// <param name="take">Max number of entries to take (for paging). Use null to take all.</param>
    /// <param name="ct">Cancellation token.</param>
    IAsyncEnumerable<Structured.AuditEvent> GetByRootAsync(string rootType, string rootId, DateTime? fromUtc, DateTime? toUtc, int skip = 0, int? take = null, CancellationToken ct = default);
}

internal sealed class EfCoreAuditHistoryReader(AuditStoreDbContext db) : IAuditHistoryReader
{
    public async IAsyncEnumerable<Structured.AuditEvent> GetByRootAsync(string rootType, string rootId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var query = db.Entries
                .AsNoTracking()
                .Where(e => e.RootType == rootType && e.RootId == rootId)
                .OrderBy(e => e.TimestampUtc)
                .Select(e => new { e, e.Event, Changes = e.Changes });

        await foreach (var row in query.AsAsyncEnumerable().WithCancellation(ct))
        {
            var evt = new Structured.AuditEvent
            {
                Id = row.Event!.Id,
                TimestampUtc = row.Event!.TimestampUtc,
                Actor = row.Event!.Actor,
                CorrelationId = row.Event!.CorrelationId,
                TenantId = row.Event!.TenantId,
            };

            var entry = new Structured.AuditEntry
            {
                EntityType = row.e.EntityType,
                EntityId = row.e.EntityId,
                EntityTitle = row.e.EntityTitle,
                RootType = row.e.RootType,
                RootId = row.e.RootId,
                RootTitle = row.e.RootTitle
            };

            foreach (var ch in row.Changes)
            {
                entry.Changes.Add(new Structured.AuditChange
                {
                    ChangeType = (Structured.AuditChangeType)ch.ChangeType,
                    PropertyPath = ch.PropertyPath,
                    DisplayName = ch.DisplayName,
                    Old = ch.Old,
                    New = ch.New,
                    CollectionDisplay = ch.CollectionDisplay,
                    RelatedEntityType = ch.RelatedEntityType,
                    RelatedEntityId = ch.RelatedEntityId,
                    RelatedEntityTitle = ch.RelatedEntityTitle,
                    Message = ch.Message
                });
            }

            evt.Entries.Add(entry);
            yield return evt;
        }
    }

    public async IAsyncEnumerable<Structured.AuditEvent> GetByRootAsync(
        string rootType,
        string rootId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int skip = 0,
        int? take = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var query = db.Entries.AsNoTracking().Where(e => e.RootType == rootType && e.RootId == rootId);
        if (fromUtc is not null) query = query.Where(e => e.TimestampUtc >= fromUtc);
        if (toUtc is not null) query = query.Where(e => e.TimestampUtc < toUtc);
        query = query.OrderBy(e => e.TimestampUtc);
        if (skip > 0) query = query.Skip(skip);
        if (take is not null) query = query.Take(take.Value);

        var projected = query.Select(e => new { e, e.Event, Changes = e.Changes });
        await foreach (var row in projected.AsAsyncEnumerable().WithCancellation(ct))
        {
            var evt = new Structured.AuditEvent
            {
                Id = row.Event!.Id,
                TimestampUtc = row.Event!.TimestampUtc,
                Actor = row.Event!.Actor,
                CorrelationId = row.Event!.CorrelationId,
                TenantId = row.Event!.TenantId,
            };

            var entry = new Structured.AuditEntry
            {
                EntityType = row.e.EntityType,
                EntityId = row.e.EntityId,
                EntityTitle = row.e.EntityTitle,
                RootType = row.e.RootType,
                RootId = row.e.RootId,
                RootTitle = row.e.RootTitle
            };

            foreach (var ch in row.Changes)
            {
                entry.Changes.Add(new Structured.AuditChange
                {
                    ChangeType = (Structured.AuditChangeType)ch.ChangeType,
                    PropertyPath = ch.PropertyPath,
                    DisplayName = ch.DisplayName,
                    Old = ch.Old,
                    New = ch.New,
                    CollectionDisplay = ch.CollectionDisplay,
                    RelatedEntityType = ch.RelatedEntityType,
                    RelatedEntityId = ch.RelatedEntityId,
                    RelatedEntityTitle = ch.RelatedEntityTitle,
                    Message = ch.Message
                });
            }

            evt.Entries.Add(entry);
            yield return evt;
        }
    }
}
