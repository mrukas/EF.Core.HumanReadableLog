namespace EF.Core.HumanReadableLog.Attributes;

/// <summary>
/// Specifies a human-friendly display name for a property, field or entity used in audit messages.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class AuditDisplayAttribute : Attribute
{
    /// <summary>
    /// Sets a human-friendly display name for a property or entity used in audit messages.
    /// </summary>
    public AuditDisplayAttribute(string displayName)
    {
        DisplayName = displayName;
    }

    /// <summary>
    /// The human-friendly name to show in messages.
    /// </summary>
    public string DisplayName { get; }
}
