using System.Collections.Concurrent;
using LogViewer;

namespace LogViewer.Tests
{
    /// <summary>
    /// Test double for IBaseLoggerSink that captures log events for assertion.
    /// </summary>
    public class TestBaseLoggerSink : IBaseLoggerSink
    {
        public List<LogEventArgs> ReceivedEvents { get; } = new();
        public event LogEvent? LogReceived;
        public int MaxQueueSize { get; set; } = 1000;
        public ConcurrentQueue<LogEventArgs> LogQueue { get; } = new();

        public void Write(LogEventArgs logEvent)
        {
            if (logEvent is null) return;

            ReceivedEvents.Add(logEvent);
            LogQueue.Enqueue(logEvent);
            LogReceived?.Invoke(this, logEvent);
        }

        public void Clear()
        {
            ReceivedEvents.Clear();
            while (LogQueue.TryDequeue(out _)) { }
        }
    }
}
