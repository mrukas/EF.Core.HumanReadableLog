using EF.Core.HumanReadableLog.Localization;

namespace EF.Core.HumanReadableLog.Structured.Formatting;

/// <summary>
/// Formats structured audit events into human-readable lines, optionally including parent context.
/// </summary>
public static class AuditHistoryFormatter
{
    /// <summary>
    /// Formats events using the default English localizer.
    /// </summary>
    public static IEnumerable<string> FormatWithParentContext(IEnumerable<AuditEvent> events)
        => FormatWithParentContext(events, new EnglishAuditLocalizer());

    /// <summary>
    /// Formats events for display in the context of a root, prefixing each change with the parent entry title/type when available.
    /// Pass a localizer to control language (e.g., EnglishAuditLocalizer or GermanAuditLocalizer).
    /// Example: "Week 1 (StatusReport) -> Note (Comment) was removed from Comments".
    /// </summary>
    public static IEnumerable<string> FormatWithParentContext(IEnumerable<AuditEvent> events, IAuditLocalizer localizer)
    {
        foreach (var evt in events)
        {
            foreach (var entry in evt.Entries)
            {
                var entryTitle = string.IsNullOrWhiteSpace(entry.EntityTitle) ? null : entry.EntityTitle;
                var entryType = string.IsNullOrWhiteSpace(entry.EntityType) ? null : entry.EntityType;

                foreach (var change in entry.Changes)
                {
                    var msg = change.Message;
                    if (string.IsNullOrWhiteSpace(msg))
                    {
                        // Fallback: build a minimal message from change data if Message wasn't provided
                        switch (change.ChangeType)
                        {
                            case AuditChangeType.Property:
                                msg = localizer.PropertyChangeTemplate
                                    .Replace("{DisplayName}", change.DisplayName ?? string.Empty)
                                    .Replace("{Old}", change.Old ?? localizer.NullSymbol)
                                    .Replace("{New}", change.New ?? localizer.NullSymbol);
                                break;
                            case AuditChangeType.CollectionAdded:
                                msg = localizer.CollectionAddedTemplate
                                    .Replace("{Title}", change.RelatedEntityTitle ?? change.RelatedEntityType ?? string.Empty)
                                    .Replace("{EntitySingular}", change.RelatedEntityType ?? string.Empty)
                                    .Replace("{CollectionDisplay}", change.CollectionDisplay ?? string.Empty);
                                break;
                            case AuditChangeType.CollectionRemoved:
                                msg = localizer.CollectionRemovedTemplate
                                    .Replace("{Title}", change.RelatedEntityTitle ?? change.RelatedEntityType ?? string.Empty)
                                    .Replace("{EntitySingular}", change.RelatedEntityType ?? string.Empty)
                                    .Replace("{CollectionDisplay}", change.CollectionDisplay ?? string.Empty);
                                break;
                            case AuditChangeType.Deleted:
                                msg = "Deleted"; // No entity context in change; message usually set already by interceptor
                                break;
                            default:
                                msg = string.Empty;
                                break;
                        }
                    }
                    // Build context chain: Root -> Parent -> Entry (if available)
                    var labels = new List<string>(capacity: 3);
                    if (!string.IsNullOrWhiteSpace(entry.RootTitle) || !string.IsNullOrWhiteSpace(entry.RootType))
                    {
                        var rootLabel = string.IsNullOrWhiteSpace(entry.RootTitle)
                            ? entry.RootType ?? string.Empty
                            : (string.IsNullOrWhiteSpace(entry.RootType) ? entry.RootTitle : $"{entry.RootTitle} ({entry.RootType})");
                        if (!string.IsNullOrWhiteSpace(rootLabel)) labels.Add(rootLabel);
                    }
                    if (!string.IsNullOrWhiteSpace(change.ParentEntityTitle) || !string.IsNullOrWhiteSpace(change.ParentEntityType))
                    {
                        var parentLabel = string.IsNullOrWhiteSpace(change.ParentEntityTitle)
                            ? change.ParentEntityType ?? string.Empty
                            : (string.IsNullOrWhiteSpace(change.ParentEntityType) ? change.ParentEntityTitle : $"{change.ParentEntityTitle} ({change.ParentEntityType})");
                        if (!string.IsNullOrWhiteSpace(parentLabel)) labels.Add(parentLabel);
                    }
                    var entryLabel = entryTitle is null ? (entryType ?? string.Empty) : (entryType is null ? entryTitle : $"{entryTitle} ({entryType})");
                    if (!string.IsNullOrWhiteSpace(entryLabel)) labels.Add(entryLabel);

                    var prefix = labels.Count > 0 ? string.Join(" -> ", labels) + " -> " : string.Empty;
                    yield return prefix + msg;
                }
            }
        }
    }
}
