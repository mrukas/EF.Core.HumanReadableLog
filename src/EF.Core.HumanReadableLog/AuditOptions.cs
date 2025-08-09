namespace EF.Core.HumanReadableLog;

/// <summary>
/// Configures audit message templates and behavior.
/// </summary>
public sealed class AuditOptions
{
    /// <summary>
    /// If true, include unchanged properties explicitly marked for logging.
    /// </summary>
    public bool IncludeUnchangedMarkedProperties { get; set; }

    /// <summary>
    /// When true, deletes will include all current values with a label "gelöscht".
    /// </summary>
    public bool VerboseDelete { get; set; } = true;

    /// <summary>
    /// Customize message for collection add: "{Title} ({EntitySingular}) wurde zu {CollectionDisplay} hinzugefügt".
    /// </summary>
    public string CollectionAddedTemplate { get; set; } = "{Title} ({EntitySingular}) wurde zu {CollectionDisplay} hinzugefügt";

    /// <summary>
    /// Customize message for collection remove: "{Title} ({EntitySingular}) wurde von {CollectionDisplay} entfernt".
    /// </summary>
    public string CollectionRemovedTemplate { get; set; } = "{Title} ({EntitySingular}) wurde von {CollectionDisplay} entfernt";

    /// <summary>
    /// Customize property change template: "{DisplayName}: {Old} -> {New}".
    /// </summary>
    public string PropertyChangeTemplate { get; set; } = "{DisplayName}: {Old} -> {New}";
}
