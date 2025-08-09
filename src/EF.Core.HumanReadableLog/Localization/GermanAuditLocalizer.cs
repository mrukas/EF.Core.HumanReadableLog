namespace EF.Core.HumanReadableLog.Localization;

/// <summary>
/// Deutsche Templates und Formatierungen für Audit-Nachrichten.
/// </summary>
public sealed class GermanAuditLocalizer : IAuditLocalizer
{
    /// <inheritdoc />
    public string PropertyChangeTemplate => "{DisplayName}: {Old} -> {New}";
    /// <inheritdoc />
    public string CollectionAddedTemplate => "{Title} ({EntitySingular}) wurde zu {CollectionDisplay} hinzugefügt";
    /// <inheritdoc />
    public string CollectionRemovedTemplate => "{Title} ({EntitySingular}) wurde von {CollectionDisplay} entfernt";
    /// <inheritdoc />
    public string DeletedTemplate => "{Title} ({EntitySingular}) gelöscht";

    /// <inheritdoc />
    public string NullSymbol => "∅";
    /// <inheritdoc />
    public string FormatBool(bool value) => value ? "Ja" : "Nein";
}
