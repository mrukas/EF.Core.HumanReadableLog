using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EF.Core.HumanReadableLog.Structured;

internal sealed class PreEntrySnapshot
{
    public required EntityEntry Entry { get; init; }
    public required EntityState State { get; init; }
    public required List<PrePropertyDelta> Properties { get; init; }
    public required List<PreCollectionDelta> Collections { get; init; }
}
