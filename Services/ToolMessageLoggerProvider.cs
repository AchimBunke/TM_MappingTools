using Microsoft.Extensions.Logging;

namespace TM_MappingTools.Services;

/// <summary>
/// Forwards unhandled exceptions logged by the framework (event handlers, rendering, ...)
/// to the output panel.
/// </summary>
public sealed class ToolMessageLoggerProvider : ILoggerProvider
{
    private readonly ToolMessageService _messages;

    public ToolMessageLoggerProvider(ToolMessageService messages) => _messages = messages;

    public ILogger CreateLogger(string categoryName) => new ToolMessageLogger(_messages);

    public void Dispose() { }

    private sealed class ToolMessageLogger : ILogger
    {
        private readonly ToolMessageService _messages;

        public ToolMessageLogger(ToolMessageService messages) => _messages = messages;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel) || exception == null) return;
            _messages.ReportUnhandled(exception);
        }
    }
}
