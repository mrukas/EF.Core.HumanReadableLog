namespace EF.Core.HumanReadableLog.Structured;

internal sealed class InMemoryAuditBuffer : IAuditBuffer
{
    private readonly List<AuditEvent> _events = new();
    public void Add(AuditEvent evt) => _events.Add(evt);
    public IReadOnlyList<AuditEvent> Drain() => _events.ToList();
    public void Clear() => _events.Clear();
}
