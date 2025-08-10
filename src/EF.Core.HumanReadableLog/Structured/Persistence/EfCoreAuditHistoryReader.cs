using Microsoft.EntityFrameworkCore;

namespace EF.Core.HumanReadableLog.Structured.Persistence;

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
                    ParentEntityType = ch.ParentEntityType,
                    ParentEntityId = ch.ParentEntityId,
                    ParentEntityTitle = ch.ParentEntityTitle,
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
                    ParentEntityType = ch.ParentEntityType,
                    ParentEntityId = ch.ParentEntityId,
                    ParentEntityTitle = ch.ParentEntityTitle,
                    Message = ch.Message
                });
            }

            evt.Entries.Add(entry);
            yield return evt;
        }
    }
}
