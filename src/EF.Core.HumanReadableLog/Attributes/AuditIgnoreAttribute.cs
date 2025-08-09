namespace EF.Core.HumanReadableLog.Attributes;

/// <summary>
/// Marks a property to be ignored by the audit logger.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class AuditIgnoreAttribute : Attribute
{ }
