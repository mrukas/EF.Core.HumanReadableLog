using EF.Core.HumanReadableLog.Localization;

namespace EF.Core.HumanReadableLog;

/// <summary>
/// Configures audit message templates and behavior.
/// </summary>
public sealed class AuditOptions
{
    /// <summary>
    /// Localizer providing language-specific templates and formatting. Defaults to English.
    /// </summary>
    public IAuditLocalizer Localizer { get; set; } = new EnglishAuditLocalizer();
    /// <summary>
    /// If true, include unchanged properties explicitly marked for logging.
    /// </summary>
    public bool IncludeUnchangedMarkedProperties { get; set; }

    /// <summary>
    /// When true, deletes will include all current values (message is localized by the configured localizer).
    /// </summary>
    public bool VerboseDelete { get; set; } = true;

    /// <summary>
    /// Customize message for collection add. If left empty, the current localizer's default is used
    /// (e.g., English: "{Title} ({EntitySingular}) was added to {CollectionDisplay}").
    /// </summary>
    public string CollectionAddedTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Customize message for collection remove. If left empty, the current localizer's default is used
    /// (e.g., English: "{Title} ({EntitySingular}) was removed from {CollectionDisplay}").
    /// </summary>
    public string CollectionRemovedTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Customize property change template: "{DisplayName}: {Old} -> {New}". If left empty, the current localizer's
    /// default is used.
    /// </summary>
    public string PropertyChangeTemplate { get; set; } = string.Empty;
}
