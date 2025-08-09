using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EF.Core.HumanReadableLog;

/// <summary>
/// Receives human-readable audit messages produced during SaveChanges.
/// </summary>
public interface IAuditEventSink
{
    /// <summary>
    /// Called with human-readable audit messages for a SaveChanges invocation.
    /// </summary>
    /// <param name="messages">Ordered messages describing the change set.</param>
    /// <param name="cancellationToken">Cancellation token for the asynchronous operation.</param>
    Task WriteAsync(IEnumerable<string> messages, CancellationToken cancellationToken = default);
}
