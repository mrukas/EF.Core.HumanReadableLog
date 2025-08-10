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

            // Fallback: if principal isn't tracked, anchor using FK values on the dependent
            string? fallbackId = null;
            var depValues = fkToPrincipal.Properties
                .Select(p => {
                    var prop = entry.Property(p.Name);
                    var cur = prop.CurrentValue;
                    var orig = prop.OriginalValue;
                    var clr = p.ClrType;
                    bool IsDefault(object? x) => x is null || (clr.IsValueType && Equals(x, Activator.CreateInstance(clr)));
                    // On delete EF may null/reset FKs; prefer original if current is default
                    var val = (entry.State == Microsoft.EntityFrameworkCore.EntityState.Deleted && IsDefault(cur) && !IsDefault(orig)) ? orig : cur;
                    return val ?? orig;
                })
                .ToArray();
            if (depValues.Length > 0 && depValues.Any(v => v is not null))
            {
                fallbackId = string.Join("|", depValues.Select(v => v is null ? "∅" : v.ToString()));
            }
            if (!string.IsNullOrEmpty(fallbackId))
            {
                yield return new AuditAnchor
                {
                    RootType = fkToPrincipal.PrincipalEntityType.ClrType.Name,
                    RootId = fallbackId,
                    RootTitle = null
                };
                yield break;
            }

            // As a last resort, try getting FK values from the database (entity still exists before delete is committed)
            if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Deleted)
            {
                string? dbFallbackId = GetFkCompositeIdFromDb(entry, fkToPrincipal);
                if (!string.IsNullOrEmpty(dbFallbackId))
                {
                    yield return new AuditAnchor
                    {
                        RootType = fkToPrincipal.PrincipalEntityType.ClrType.Name,
                        RootId = dbFallbackId,
                        RootTitle = null
                    };
                    yield break;
                }
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

    private static string? GetFkCompositeIdFromDb(EntityEntry entry, IForeignKey fk)
    {
        try
        {
            var dbValues = entry.GetDatabaseValues();
            if (dbValues is null) return null;
            var vals = fk.Properties.Select(p => dbValues[p]).ToArray();
            if (vals.Length == 0 || !vals.Any(v => v is not null)) return null;
            return string.Join("|", vals.Select(v => v is null ? "∅" : v.ToString()));
        }
        catch { return null; }
    }
}
