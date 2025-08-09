using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EF.Core.HumanReadableLog.Structured;

internal sealed class DefaultEntityKeyFormatter : IEntityKeyFormatter
{
    public string FormatEntityKey(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null) return entry.Entity.GetHashCode().ToString();
        var parts = key.Properties
            .Select(p => entry.Property(p.Name).CurrentValue)
            .Select(v => v is null ? "∅" : v.ToString());
        return string.Join("|", parts);
    }
}
