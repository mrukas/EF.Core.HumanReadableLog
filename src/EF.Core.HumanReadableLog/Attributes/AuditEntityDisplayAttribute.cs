namespace EF.Core.HumanReadableLog.Attributes;

/// <summary>
/// Provides localized singular and plural display names for an entity used in audit messages.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class AuditEntityDisplayAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance with singular and optional plural display names.
    /// </summary>
    /// <param name="singular">Singular display name (e.g., "Pet").</param>
    /// <param name="plural">Optional plural; defaults to singular + "s".</param>
    public AuditEntityDisplayAttribute(string singular, string? plural = null)
    {
        Singular = singular;
        Plural = plural ?? singular + "s";
    }

    /// <summary>Singular display name for the entity (e.g., "Pet").</summary>
    public string Singular { get; }
    /// <summary>Plural display name for the entity (e.g., "Pets").</summary>
    public string Plural { get; }
}
