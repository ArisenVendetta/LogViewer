using System.Collections.Concurrent;

namespace LogViewer
{
    /// <summary>
    /// Defines a sink interface for receiving log events destined for the LogViewer UI.
    /// Implementations should be thread-safe and support multiple concurrent writers.
    /// </summary>
    public interface IBaseLoggerSink
    {
        /// <summary>
        /// Event raised when a log message is received.
        /// Subscribers (like LogControl) can use this to update the UI in real-time.
        /// </summary>
        event LogEvent? LogReceived;

        /// <summary>
        /// Writes a log event to the sink.
        /// </summary>
        /// <param name="logEvent">The log event to write.</param>
        void Write(LogEventArgs logEvent);

        /// <summary>
        /// Gets or sets the maximum queue size before oldest entries are dropped.
        /// </summary>
        int MaxQueueSize { get; set; }

        /// <summary>
        /// Gets the internal log queue for access by LogControl.
        /// </summary>
        ConcurrentQueue<LogEventArgs> LogQueue { get; }
    }
}
