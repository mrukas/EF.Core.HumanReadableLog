using System.Reflection;
using EF.Core.HumanReadableLog.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EF.Core.HumanReadableLog;

/// <summary>
/// EF Core SaveChanges interceptor that produces human-readable audit messages for property and relationship changes.
/// </summary>
public sealed class AuditingSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IAuditEventSink _sink;
    private readonly AuditOptions _options;

    /// <summary>
    /// Initializes a new instance of the interceptor.
    /// </summary>
    /// <param name="sink">The sink receiving generated audit messages.</param>
    /// <param name="options">Templates and settings controlling output.</param>
    public AuditingSaveChangesInterceptor(IAuditEventSink sink, AuditOptions? options = null)
    {
        _sink = sink;
        _options = options ?? new AuditOptions();
    }

    /// <inheritdoc />
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var messages = BuildAuditMessages(eventData.Context.ChangeTracker);
        if (messages.Count > 0)
        {
            await _sink.WriteAsync(messages, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private List<string> BuildAuditMessages(ChangeTracker tracker)
    {
        var messages = new List<string>();

        foreach (var entry in tracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var entityType = entry.Entity.GetType();
            var (entitySingular, entityPlural) = ResolveEntityDisplay(entityType);

            if (entry.State == EntityState.Modified)
            {
                foreach (var prop in entry.Properties)
                {
                    if (!prop.IsModified) continue;
                    if (ShouldIgnore(prop.Metadata.PropertyInfo)) continue;

                    var displayName = ResolvePropertyDisplay(prop.Metadata.PropertyInfo);
                    var (oldVal, newVal) = (FormatValue(prop.OriginalValue), FormatValue(prop.CurrentValue));
                    messages.Add(_options.PropertyChangeTemplate
                        .Replace("{DisplayName}", displayName)
                        .Replace("{Old}", oldVal)
                        .Replace("{New}", newVal));
                }
            }

            if (entry.State == EntityState.Added)
            {
                // For added entities we don't log property deltas; collection changes will be handled by relationships below.
            }

            if (entry.State == EntityState.Deleted && _options.VerboseDelete && !IsManyToManyJoin(entry))
            {
                var title = ResolveEntityTitle(entry.Entity) ?? entitySingular;
                messages.Add($"{title} ({entitySingular}) gelöscht");
            }

            // Collection-like messages via FK with principal collection navigation (one-to-many)
            if (entry.State is EntityState.Added or EntityState.Deleted)
            {
                // Many-to-many join entity (shared type, 2 FKs) handling
                TryHandleManyToMany(entry, messages);

                var fks = entry.Metadata.GetForeignKeys();
                foreach (var fk in fks)
                {
                    var principalCollection = fk.PrincipalToDependent; // collection navigation on principal
                    if (principalCollection is null || !principalCollection.IsCollection)
                        continue;

                    var collectionPi = principalCollection.PropertyInfo;
                    var resolved = collectionPi is null ? null : ResolvePropertyDisplay(collectionPi);
                    var collectionDisplay = string.IsNullOrWhiteSpace(resolved) ? principalCollection.Name : resolved;

                    var childEntity = entry.Entity;
                    var childSingular = ResolveEntityDisplay(childEntity.GetType()).singular;
                    var childTitle = ResolveEntityTitle(childEntity) ?? childSingular;

                    if (entry.State == EntityState.Added)
                    {
                        messages.Add(_options.CollectionAddedTemplate
                            .Replace("{Title}", childTitle)
                            .Replace("{EntitySingular}", childSingular)
                            .Replace("{CollectionDisplay}", collectionDisplay));
                    }
                    else // Deleted
                    {
                        messages.Add(_options.CollectionRemovedTemplate
                            .Replace("{Title}", childTitle)
                            .Replace("{EntitySingular}", childSingular)
                            .Replace("{CollectionDisplay}", collectionDisplay));
                    }
                }
            }
        }

        return messages;
    }

    private void TryHandleManyToMany(EntityEntry entry, List<string> messages)
    {
        if (!IsManyToManyJoin(entry)) return;
        var fks = entry.Metadata.GetForeignKeys().ToArray();

        // Resolve principal entries (both sides)
        var refs = entry.References.ToList();
        for (int i = 0; i < 2; i++)
        {
            var thisFk = fks[i];
            var otherFk = fks[1 - i];
            var principalType = thisFk.PrincipalEntityType;
            var otherType = otherFk.PrincipalEntityType;

            var principalEntry = refs.FirstOrDefault(r => r.TargetEntry?.Metadata == principalType)?.TargetEntry
                ?? FindPrincipalByKey(entry.Context.ChangeTracker, thisFk, entry);
            var otherEntry = refs.FirstOrDefault(r => r.TargetEntry?.Metadata == otherType)?.TargetEntry
                ?? FindPrincipalByKey(entry.Context.ChangeTracker, otherFk, entry);
            if (principalEntry is null || otherEntry is null) continue;

            // Find the skip navigation on principal that points to the other side
            var skipNav = principalType.GetSkipNavigations().FirstOrDefault(sn => sn.TargetEntityType == otherType);
            if (skipNav is null || !skipNav.IsCollection) continue;

            var pi = skipNav.PropertyInfo;
            var resolved = pi is null ? null : ResolvePropertyDisplay(pi);
            var collectionDisplay = string.IsNullOrWhiteSpace(resolved) ? skipNav.Name : resolved;

            var childEntity = otherEntry.Entity;
            var childSingular = ResolveEntityDisplay(childEntity.GetType()).singular;
            var childTitle = ResolveEntityTitle(childEntity) ?? childSingular;

            if (entry.State == EntityState.Added)
            {
                messages.Add(_options.CollectionAddedTemplate
                    .Replace("{Title}", childTitle)
                    .Replace("{EntitySingular}", childSingular)
                    .Replace("{CollectionDisplay}", collectionDisplay));
            }
            else if (entry.State == EntityState.Deleted)
            {
                messages.Add(_options.CollectionRemovedTemplate
                    .Replace("{Title}", childTitle)
                    .Replace("{EntitySingular}", childSingular)
                    .Replace("{CollectionDisplay}", collectionDisplay));
            }
        }
    }

    private static bool IsManyToManyJoin(EntityEntry entry)
    {
        try
        {
            var fks = entry.Metadata.GetForeignKeys();
            if (fks.Count() == 2 && entry.Entity is System.Collections.IDictionary) return true;
        }
        catch { }
        return false;
    }

    private static EntityEntry? FindPrincipalByKey(ChangeTracker tracker, IForeignKey fk, EntityEntry joinEntry)
    {
        var depProps = fk.Properties;
        var principalKeyProps = fk.PrincipalKey.Properties;
        var depValues = depProps.Select(p => joinEntry.Property(p.Name).CurrentValue).ToArray();
        foreach (var e in tracker.Entries().Where(e => e.Metadata == fk.PrincipalEntityType))
        {
            bool match = true;
            for (int i = 0; i < principalKeyProps.Count; i++)
            {
                var pkProp = principalKeyProps[i];
                var val = e.Property(pkProp.Name).CurrentValue;
                if (!object.Equals(val, depValues[i]))
                {
                    match = false; break;
                }
            }
            if (match) return e;
        }
        return null;
    }

    private static bool ShouldIgnore(PropertyInfo? pi)
    {
        if (pi is null) return false;
        return pi.GetCustomAttribute<AuditIgnoreAttribute>() is not null;
    }

    private static string ResolvePropertyDisplay(PropertyInfo? pi)
    {
        if (pi is null) return string.Empty;
        var attr = pi.GetCustomAttribute<AuditDisplayAttribute>();
        return attr?.DisplayName ?? pi.Name;
    }

    private static (string singular, string plural) ResolveEntityDisplay(Type entityType)
    {
        var attr = entityType.GetCustomAttribute<AuditEntityDisplayAttribute>();
        if (attr is not null) return (attr.Singular, attr.Plural);
        var name = entityType.Name;
        return (name, name + "s");
    }

    private static string? ResolveEntityTitle(object entity)
    {
        // 1) Template on entity
        var templateAttr = entity.GetType().GetCustomAttribute<AuditEntityTitleTemplateAttribute>();
        if (templateAttr is not null)
        {
            var rendered = RenderTemplate(templateAttr.Template, entity);
            if (!string.IsNullOrWhiteSpace(rendered)) return rendered;
        }

        // 2) Single property marked as title
        var pi = entity.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(p => p.GetCustomAttribute<AuditEntityTitleAttribute>() is not null);
        if (pi is not null)
        {
            var val = pi.GetValue(entity);
            var s = val?.ToString();
            if (!string.IsNullOrWhiteSpace(s)) return s;
        }

        return null;
    }

    private static string RenderTemplate(string template, object model)
    {
        // Supports placeholders like {Property} and nested {Owner.Name}
        // Not a full parser—simple scan for {...}
        var result = template;
        int start = 0;
        while ((start = result.IndexOf('{', start)) >= 0)
        {
            var end = result.IndexOf('}', start + 1);
            if (end < 0) break;
            var token = result.Substring(start, end - start + 1);
            var path = token.Trim('{', '}').Trim();
            var value = ResolvePath(model, path);
            result = result.Remove(start, end - start + 1).Insert(start, value);
            start += value.Length;
        }
        return result;
    }

    private static string ResolvePath(object? current, string path)
    {
        if (current is null) return string.Empty;
        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        object? value = current;
        foreach (var seg in segments)
        {
            if (value is null) break;
            var pi = value.GetType().GetProperty(seg, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            value = pi?.GetValue(value);
        }
        return FormatValue(value);
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "∅",
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            DateOnly d => d.ToString("yyyy-MM-dd"),
            TimeOnly t => t.ToString("HH:mm:ss"),
            bool b => b ? "Ja" : "Nein",
            Enum e => e.ToString(),
            _ => value.ToString() ?? string.Empty
        };
    }
}
