using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EF.Core.HumanReadableLog.Structured;

internal sealed class DefaultAuditRootResolver : IAuditRootResolver
{
    public IEnumerable<AuditAnchor> ResolveAnchors(EntityEntry entry)
    {
        // Many-to-many join heuristic: shared-type entity with exactly 2 FKs
        if (LooksLikeManyToManyJoin(entry))
        {
            foreach (var fk in entry.Metadata.GetForeignKeys())
            {
                var principalType = fk.PrincipalEntityType.ClrType;
                var principalEntry = entry.References
                    .FirstOrDefault(r => r.TargetEntry?.Metadata == fk.PrincipalEntityType)?.TargetEntry;
                var id = principalEntry is null ? "" : new DefaultEntityKeyFormatter().FormatEntityKey(principalEntry);
                yield return new AuditAnchor
                {
                    RootType = principalType.Name,
                    RootId = id,
                    RootTitle = null
                };
            }
            yield break;
        }

        // If entity has a principal with collection (typical 1:n), use principal as root
        var fkToPrincipal = entry.Metadata.GetForeignKeys()
            .FirstOrDefault();
        if (fkToPrincipal?.PrincipalEntityType is not null)
        {
            var principalEntry = entry.References
                .FirstOrDefault(r => r.TargetEntry?.Metadata == fkToPrincipal.PrincipalEntityType)?.TargetEntry
                ?? FindPrincipalByKey(entry.Context.ChangeTracker, fkToPrincipal, entry);
            if (principalEntry is not null)
            {
                var id = new DefaultEntityKeyFormatter().FormatEntityKey(principalEntry);
                yield return new AuditAnchor
                {
                    RootType = fkToPrincipal.PrincipalEntityType.ClrType.Name,
                    RootId = id,
                    RootTitle = null
                };
                yield break;
            }
        }

        // Fallback: anchor to the entity itself
        yield return new AuditAnchor
        {
            RootType = entry.Entity.GetType().Name,
            RootId = new DefaultEntityKeyFormatter().FormatEntityKey(entry),
            RootTitle = null
        };
    }

    private static bool LooksLikeManyToManyJoin(EntityEntry entry)
    {
        try
        {
            var fks = entry.Metadata.GetForeignKeys();
            if (fks.Count() == 2 && entry.Entity is System.Collections.IDictionary) return true;
        }
        catch { }
        return false;
    }

    private static EntityEntry? FindPrincipalByKey(ChangeTracker tracker, IForeignKey fk, EntityEntry dependentEntry)
    {
        try
        {
            var depProps = fk.Properties;
            var principalKeyProps = fk.PrincipalKey.Properties;
            var depValues = depProps.Select(p => dependentEntry.Property(p.Name).CurrentValue).ToArray();

            foreach (var e in tracker.Entries().Where(e => e.Metadata == fk.PrincipalEntityType))
            {
                bool match = true;
                for (int i = 0; i < principalKeyProps.Count; i++)
                {
                    var pkProp = principalKeyProps[i];
                    var val = e.Property(pkProp.Name).CurrentValue;
                    if (!object.Equals(val, depValues[i])) { match = false; break; }
                }
                if (match) return e;
            }
        }
        catch { }
        return null;
    }
}
