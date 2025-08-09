namespace EF.Core.HumanReadableLog.Localization;

/// <summary>
/// English templates and formatting for audit messages.
/// </summary>
public sealed class EnglishAuditLocalizer : IAuditLocalizer
{
    /// <inheritdoc />
    public string PropertyChangeTemplate => "{DisplayName}: {Old} -> {New}";
    /// <inheritdoc />
    public string CollectionAddedTemplate => "{Title} ({EntitySingular}) was added to {CollectionDisplay}";
    /// <inheritdoc />
    public string CollectionRemovedTemplate => "{Title} ({EntitySingular}) was removed from {CollectionDisplay}";
    /// <inheritdoc />
    public string DeletedTemplate => "{Title} ({EntitySingular}) deleted";

    /// <inheritdoc />
    public string NullSymbol => "∅";
    /// <inheritdoc />
    public string FormatBool(bool value) => value ? "Yes" : "No";
}
