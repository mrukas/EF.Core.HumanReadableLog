namespace EF.Core.HumanReadableLog.Attributes;

/// <summary>
/// Provides a template used to render the entity's title in messages, e.g., "{Name} ({Id})".
/// Supports nested paths like "{Owner.Name}".
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class AuditEntityTitleTemplateAttribute(string template) : Attribute
{
    /// <summary>
    /// The title template with placeholders like {Name}.
    /// </summary>
    public string Template { get; } = template;
}
