using System.Reflection;
using EF.Core.HumanReadableLog.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using EF.Core.HumanReadableLog.Structured;

namespace EF.Core.HumanReadableLog;

/// <summary>
/// EF Core SaveChanges interceptor that produces human-readable audit messages for property and relationship changes.
/// </summary>
public sealed partial class AuditingSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IAuditEventSink _sink;
    private readonly AuditOptions _options;
    private readonly IAuditBuffer _buffer;
    private readonly IAuditRootResolver _rootResolver;
    private readonly IAuditActorProvider _actorProvider;
    private readonly ICorrelationIdProvider _corrProvider;
    private readonly ITenantProvider _tenantProvider;
    private readonly IStructuredAuditEventSink? _structuredSink;

    /// <summary>
    /// Initializes a new instance of the interceptor.
    /// </summary>
    /// <param name="sink">The sink receiving generated audit messages.</param>
    /// <param name="options">Templates and settings controlling output.</param>
    /// <param name="buffer">Optional buffer for structured audit events.</param>
    /// <param name="rootResolver">Optional resolver for root/anchor entities.</param>
    /// <param name="actorProvider">Optional actor provider.</param>
    /// <param name="corrProvider">Optional correlation id provider.</param>
    /// <param name="tenantProvider">Optional tenant provider.</param>
    /// <param name="structuredSink">Optional structured sink for persistence.</param>
    public AuditingSaveChangesInterceptor(IAuditEventSink sink, AuditOptions? options = null,
        IAuditBuffer? buffer = null,
        IAuditRootResolver? rootResolver = null,
        IAuditActorProvider? actorProvider = null,
        ICorrelationIdProvider? corrProvider = null,
        ITenantProvider? tenantProvider = null,
        IStructuredAuditEventSink? structuredSink = null)
    {
        _sink = sink;
        _options = options ?? new AuditOptions();
        _buffer = buffer ?? new InMemoryAuditBuffer();
        _rootResolver = rootResolver ?? _options.RootResolver ?? new DefaultAuditRootResolver();
        _actorProvider = actorProvider ?? _options.ActorProvider ?? new NullActorProvider();
        _corrProvider = corrProvider ?? _options.CorrelationIdProvider ?? new DefaultCorrelationIdProvider();
        _tenantProvider = tenantProvider ?? _options.TenantProvider ?? new NullTenantProvider();
        _structuredSink = structuredSink ?? _options.StructuredSink; // may be null if not configured
    }

    /// <inheritdoc />
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var tracker = eventData.Context.ChangeTracker;
        var messages = BuildAuditMessages(tracker);
        if (messages.Count > 0)
        {
            await _sink.WriteAsync(messages, cancellationToken);
        }

        // Build structured snapshot and buffer for flushing after successful save
        if (_structuredSink is not null)
        {
            var evt = BuildStructuredEvent(tracker);
            if (evt.Entries.Count > 0)
            {
                _buffer.Add(evt);
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (_structuredSink is not null)
        {
            var events = _buffer.Drain();
            if (events.Count > 0)
            {
                await _structuredSink.WriteAsync(events, cancellationToken);
                _buffer.Clear();
            }
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _buffer.Clear();
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
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
                    var template = string.IsNullOrWhiteSpace(_options.PropertyChangeTemplate)
                        ? _options.Localizer.PropertyChangeTemplate
                        : _options.PropertyChangeTemplate;
                    messages.Add(template
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
                var template = _options.Localizer.DeletedTemplate;
                messages.Add(template
                    .Replace("{Title}", title)
                    .Replace("{EntitySingular}", entitySingular));
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
                        var template = string.IsNullOrWhiteSpace(_options.CollectionAddedTemplate)
                            ? _options.Localizer.CollectionAddedTemplate
                            : _options.CollectionAddedTemplate;
                        messages.Add(template
                            .Replace("{Title}", childTitle)
                            .Replace("{EntitySingular}", childSingular)
                            .Replace("{CollectionDisplay}", collectionDisplay));
                    }
                    else // Deleted
                    {
                        var template = string.IsNullOrWhiteSpace(_options.CollectionRemovedTemplate)
                            ? _options.Localizer.CollectionRemovedTemplate
                            : _options.CollectionRemovedTemplate;
                        messages.Add(template
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
                var template = string.IsNullOrWhiteSpace(_options.CollectionAddedTemplate)
                    ? _options.Localizer.CollectionAddedTemplate
                    : _options.CollectionAddedTemplate;
                messages.Add(template
                    .Replace("{Title}", childTitle)
                    .Replace("{EntitySingular}", childSingular)
                    .Replace("{CollectionDisplay}", collectionDisplay));
            }
            else if (entry.State == EntityState.Deleted)
            {
                var template = string.IsNullOrWhiteSpace(_options.CollectionRemovedTemplate)
                    ? _options.Localizer.CollectionRemovedTemplate
                    : _options.CollectionRemovedTemplate;
                messages.Add(template
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

    private string? ResolveEntityTitle(object entity)
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

    private string RenderTemplate(string template, object model)
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

    private string ResolvePath(object? current, string path)
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

    private string FormatValue(object? value)
    {
        return value switch
        {
            null => _options.Localizer.NullSymbol,
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            DateOnly d => d.ToString("yyyy-MM-dd"),
            TimeOnly t => t.ToString("HH:mm:ss"),
            bool b => _options.Localizer.FormatBool(b),
            Enum e => e.ToString(),
            _ => value.ToString() ?? string.Empty
        };
    }
}

// Structured building helpers
partial class AuditingSaveChangesInterceptor
{
    private Structured.AuditEvent BuildStructuredEvent(ChangeTracker tracker)
    {
        var evt = new Structured.AuditEvent
        {
            Actor = _actorProvider.GetActor(),
            CorrelationId = _corrProvider.GetCorrelationId(),
            TenantId = _tenantProvider.GetTenantId(),
        };

        foreach (var entry in tracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var anchors = _rootResolver.ResolveAnchors(entry).ToList();
            if (anchors.Count == 0) continue;

            var entityType = entry.Entity.GetType();
            var entityId = new Structured.DefaultEntityKeyFormatter().FormatEntityKey(entry);
            var title = ResolveEntityTitle(entry.Entity);

            foreach (var anchor in anchors)
            {
                var e = new Structured.AuditEntry
                {
                    EntityType = entityType.Name,
                    EntityId = entityId,
                    EntityTitle = title,
                    RootType = anchor.RootType,
                    RootId = anchor.RootId,
                    RootTitle = anchor.RootTitle,
                };

                // Property changes
                if (entry.State == EntityState.Modified)
                {
                    foreach (var prop in entry.Properties)
                    {
                        if (!prop.IsModified) continue;
                        if (ShouldIgnore(prop.Metadata.PropertyInfo)) continue;
                        var display = ResolvePropertyDisplay(prop.Metadata.PropertyInfo);
                        var oldStr = FormatValue(prop.OriginalValue);
                        var newStr = FormatValue(prop.CurrentValue);
                        var template = string.IsNullOrWhiteSpace(_options.PropertyChangeTemplate)
                            ? _options.Localizer.PropertyChangeTemplate
                            : _options.PropertyChangeTemplate;
                        e.Changes.Add(new Structured.AuditChange
                        {
                            ChangeType = Structured.AuditChangeType.Property,
                            PropertyPath = prop.Metadata.Name,
                            DisplayName = display,
                            Old = oldStr,
                            New = newStr,
                            Message = template.Replace("{DisplayName}", display).Replace("{Old}", oldStr).Replace("{New}", newStr)
                        });
                    }
                }

                // Delete
                if (entry.State == EntityState.Deleted && _options.VerboseDelete && !IsManyToManyJoin(entry))
                {
                    var (singular, _) = ResolveEntityDisplay(entityType);
                    var template = _options.Localizer.DeletedTemplate;
                    var msg = template.Replace("{Title}", title ?? singular).Replace("{EntitySingular}", singular);
                    e.Changes.Add(new Structured.AuditChange
                    {
                        ChangeType = Structured.AuditChangeType.Deleted,
                        Message = msg
                    });
                }

                // Collection add/remove via FK and many-to-many
                if (entry.State is EntityState.Added or EntityState.Deleted)
                {
                    // Many-to-many
                    if (IsManyToManyJoin(entry))
                    {
                        var fks = entry.Metadata.GetForeignKeys().ToArray();
                        var refs = entry.References.ToList();
                        for (int i = 0; i < 2; i++)
                        {
                            var principalType = fks[i].PrincipalEntityType;
                            var otherType = fks[1 - i].PrincipalEntityType;
                            var principalEntry = refs.FirstOrDefault(r => r.TargetEntry?.Metadata == principalType)?.TargetEntry
                                ?? FindPrincipalByKey(entry.Context.ChangeTracker, fks[i], entry);
                            var otherEntry = refs.FirstOrDefault(r => r.TargetEntry?.Metadata == otherType)?.TargetEntry
                                ?? FindPrincipalByKey(entry.Context.ChangeTracker, fks[1 - i], entry);
                            if (principalEntry is null || otherEntry is null) continue;

                            var skipNav = principalType.GetSkipNavigations().FirstOrDefault(sn => sn.TargetEntityType == otherType);
                            if (skipNav is null || !skipNav.IsCollection) continue;
                            var pi = skipNav.PropertyInfo;
                            var resolved = pi is null ? null : ResolvePropertyDisplay(pi);
                            var collectionDisplay = string.IsNullOrWhiteSpace(resolved) ? skipNav.Name : resolved;

                            var childEntity = otherEntry.Entity;
                            var childSingular = ResolveEntityDisplay(childEntity.GetType()).singular;
                            var childTitle = ResolveEntityTitle(childEntity) ?? childSingular;
                            var template = entry.State == EntityState.Added
                                ? (string.IsNullOrWhiteSpace(_options.CollectionAddedTemplate) ? _options.Localizer.CollectionAddedTemplate : _options.CollectionAddedTemplate)
                                : (string.IsNullOrWhiteSpace(_options.CollectionRemovedTemplate) ? _options.Localizer.CollectionRemovedTemplate : _options.CollectionRemovedTemplate);
                            var msg = template.Replace("{Title}", childTitle).Replace("{EntitySingular}", childSingular).Replace("{CollectionDisplay}", collectionDisplay);

                            e.Changes.Add(new Structured.AuditChange
                            {
                                ChangeType = entry.State == EntityState.Added ? Structured.AuditChangeType.CollectionAdded : Structured.AuditChangeType.CollectionRemoved,
                                CollectionDisplay = collectionDisplay,
                                RelatedEntityType = childEntity.GetType().Name,
                                RelatedEntityId = new Structured.DefaultEntityKeyFormatter().FormatEntityKey(otherEntry),
                                RelatedEntityTitle = childTitle,
                                ParentEntityType = principalType.ClrType.Name,
                                ParentEntityId = new Structured.DefaultEntityKeyFormatter().FormatEntityKey(principalEntry!),
                                ParentEntityTitle = ResolveEntityTitle(principalEntry!.Entity) ?? ResolveEntityDisplay(principalEntry!.Entity.GetType()).singular,
                                Message = msg
                            });
                        }
                    }

                    // One-to-many via FK principal collection
                    foreach (var fk in entry.Metadata.GetForeignKeys())
                    {
                        var principalCollection = fk.PrincipalToDependent;
                        if (principalCollection is null || !principalCollection.IsCollection) continue;
                        var collectionPi = principalCollection.PropertyInfo;
                        var resolved = collectionPi is null ? null : ResolvePropertyDisplay(collectionPi);
                        var collectionDisplay = string.IsNullOrWhiteSpace(resolved) ? principalCollection.Name : resolved;

                        var childEntity = entry.Entity;
                        var childSingular = ResolveEntityDisplay(childEntity.GetType()).singular;
                        var childTitle = ResolveEntityTitle(childEntity) ?? childSingular;
                        var template = entry.State == EntityState.Added
                            ? (string.IsNullOrWhiteSpace(_options.CollectionAddedTemplate) ? _options.Localizer.CollectionAddedTemplate : _options.CollectionAddedTemplate)
                            : (string.IsNullOrWhiteSpace(_options.CollectionRemovedTemplate) ? _options.Localizer.CollectionRemovedTemplate : _options.CollectionRemovedTemplate);
                        var msg = template.Replace("{Title}", childTitle).Replace("{EntitySingular}", childSingular).Replace("{CollectionDisplay}", collectionDisplay);

                        e.Changes.Add(new Structured.AuditChange
                        {
                            ChangeType = entry.State == EntityState.Added ? Structured.AuditChangeType.CollectionAdded : Structured.AuditChangeType.CollectionRemoved,
                            CollectionDisplay = collectionDisplay,
                            RelatedEntityType = childEntity.GetType().Name,
                            RelatedEntityId = new Structured.DefaultEntityKeyFormatter().FormatEntityKey(entry),
                            RelatedEntityTitle = childTitle,
                            ParentEntityType = fk.PrincipalEntityType.ClrType.Name,
                            ParentEntityId = FindPrincipalByKey(entry.Context.ChangeTracker, fk, entry) is { } p
                                ? new Structured.DefaultEntityKeyFormatter().FormatEntityKey(p)
                                : string.Empty,
                            ParentEntityTitle = FindPrincipalByKey(entry.Context.ChangeTracker, fk, entry) is { } p2
                                ? (ResolveEntityTitle(p2.Entity) ?? ResolveEntityDisplay(p2.Entity.GetType()).singular)
                                : null,
                            Message = msg
                        });
                    }
                }

                if (e.Changes.Count > 0)
                {
                    evt.Entries.Add(e);
                }
            }
        }

        return evt;
    }
}
