using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;

namespace EF.Core.HumanReadableLog.Structured;

internal sealed class PrePropertyDelta
{
    public required string PropertyName { get; init; }
    public required string DisplayName { get; init; }
    public object? Original { get; init; }
    public object? Current { get; init; }
}

internal sealed class PreCollectionDelta
{
    public required string CollectionDisplay { get; init; }
    public required string RelatedEntityType { get; init; }
    public required object? RelatedEntityKeySnapshot { get; init; }
    public required string? RelatedEntityTitle { get; init; }
    public required bool Added { get; init; }
}

internal sealed class PreEntrySnapshot
{
    public required EntityEntry Entry { get; init; }
    public required EntityState State { get; init; }
    public required List<PrePropertyDelta> Properties { get; init; }
    public required List<PreCollectionDelta> Collections { get; init; }
}

internal interface IEntityKeyFormatter
{
    string FormatEntityKey(EntityEntry entry);
}

internal sealed class DefaultEntityKeyFormatter : IEntityKeyFormatter
{
    public string FormatEntityKey(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null) return entry.Entity.GetHashCode().ToString();
        var parts = key.Properties
            .Select(p => entry.Property(p.Name).CurrentValue)
            .Select(v => v is null ? "∅" : v.ToString());
        return string.Join("|", parts);
    }
}

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
                .FirstOrDefault(r => r.TargetEntry?.Metadata == fkToPrincipal.PrincipalEntityType)?.TargetEntry;
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
}

internal sealed class NullActorProvider : IAuditActorProvider
{
    public string? GetActor() => null;
}

internal sealed class DefaultCorrelationIdProvider : ICorrelationIdProvider
{
    public string? GetCorrelationId() => System.Diagnostics.Activity.Current?.Id;
}

internal sealed class NullTenantProvider : ITenantProvider
{
    public string? GetTenantId() => null;
}
