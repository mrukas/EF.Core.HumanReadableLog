namespace EF.Core.HumanReadableLog.Structured.Persistence;

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
