using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EF.Core.HumanReadableLog.Structured;

internal interface IEntityKeyFormatter
{
    string FormatEntityKey(EntityEntry entry);
}
