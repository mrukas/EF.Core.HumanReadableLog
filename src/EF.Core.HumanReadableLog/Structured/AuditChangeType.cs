namespace EF.Core.HumanReadableLog.Structured;

/// <summary>
/// Type of change recorded.
/// </summary>
public enum AuditChangeType
{
    Property = 0,
    CollectionAdded = 1,
    CollectionRemoved = 2,
    Deleted = 3
}
