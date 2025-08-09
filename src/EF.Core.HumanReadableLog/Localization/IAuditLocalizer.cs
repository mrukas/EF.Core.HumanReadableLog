namespace EF.Core.HumanReadableLog.Localization;

/// <summary>
/// Provides language-specific templates and formatting for audit messages.
/// </summary>
public interface IAuditLocalizer
{
    /// <summary>
    /// Template used for property changes. Typical: "{DisplayName}: {Old} -> {New}".
    /// </summary>
    string PropertyChangeTemplate { get; }
    /// <summary>
    /// Template used when an item is added to a collection.
    /// </summary>
    string CollectionAddedTemplate { get; }
    /// <summary>
    /// Template used when an item is removed from a collection.
    /// </summary>
    string CollectionRemovedTemplate { get; }
    /// <summary>
    /// Template used for delete messages.
    /// </summary>
    string DeletedTemplate { get; }

    /// <summary>
    /// Symbol used to represent null values.
    /// </summary>
    string NullSymbol { get; }
    /// <summary>
    /// Formats boolean values in the target language.
    /// </summary>
    string FormatBool(bool value);
}
