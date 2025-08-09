namespace EF.Core.HumanReadableLog.Attributes;

/// <summary>
/// Marks the property to be used as the entity's title in natural language messages.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class AuditEntityTitleAttribute : Attribute
{ }
