using Microsoft.Extensions.Logging;

namespace EF.Core.HumanReadableLog.Sinks;

/// <summary>
/// Default audit sink that writes messages using Microsoft.Extensions.Logging.
/// </summary>
public sealed class LoggerAuditSink(ILogger<LoggerAuditSink> logger) : IAuditEventSink
{
    private readonly ILogger<LoggerAuditSink> _logger = logger;

    /// <inheritdoc />
    public Task WriteAsync(IEnumerable<string> messages, CancellationToken cancellationToken = default)
    {
        foreach (var msg in messages)
        {
            _logger.LogInformation("{AuditMessage}", msg);
        }
        return Task.CompletedTask;
    }
}
