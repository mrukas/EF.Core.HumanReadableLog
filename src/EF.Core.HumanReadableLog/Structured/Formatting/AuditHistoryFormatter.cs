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
                    // Build context chain: Entry as parent, and if this is a collection change with a Parent*, include that too for deeper levels
                    string prefix;
                    if (!string.IsNullOrWhiteSpace(change.ParentEntityTitle) || !string.IsNullOrWhiteSpace(change.ParentEntityType))
                    {
                        var parentLabel = string.IsNullOrWhiteSpace(change.ParentEntityTitle)
                            ? change.ParentEntityType ?? string.Empty
                            : (string.IsNullOrWhiteSpace(change.ParentEntityType) ? change.ParentEntityTitle : $"{change.ParentEntityTitle} ({change.ParentEntityType})");
                        var entryLabel = entryTitle is null ? (entryType ?? string.Empty) : (entryType is null ? entryTitle : $"{entryTitle} ({entryType})");
                        prefix = string.IsNullOrWhiteSpace(entryLabel) ? parentLabel + " -> " : parentLabel + " -> " + entryLabel + " -> ";
                    }
                    else
                    {
                        prefix = entryTitle is null ? string.Empty : (entryType is null ? entryTitle + " -> " : $"{entryTitle} ({entryType}) -> ");
                    }

                    yield return prefix + msg;
                }
            }
        }
    }
}
