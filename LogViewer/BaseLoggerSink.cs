using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace LogViewer
{
    /// <summary>
    /// Default implementation of <see cref="IBaseLoggerSink"/>.
    /// Thread-safe singleton for routing log events from LogViewerLogger instances to the LogControl UI.
    /// </summary>
    public sealed class BaseLoggerSink : IBaseLoggerSink
    {
        private static readonly Lazy<BaseLoggerSink> _instance = new(() => new BaseLoggerSink());

        private int _queueCount; // Internal atomic counter for queue size
        private readonly ConditionalWeakTable<LogEventArgs, object?> _recentEvents = [];

        /// <summary>
        /// Gets the singleton instance of <see cref="BaseLoggerSink"/>.
        /// </summary>
        public static BaseLoggerSink Instance => _instance.Value;

        /// <summary>
        /// Gets or sets the logger factory for internal logging.
        /// Set this after building the service provider to enable logging in LogControlViewModel.
        /// </summary>
        public ILoggerFactory? LoggerFactory { get; set; }

        /// <inheritdoc />
        public event LogEvent? LogReceived;

        /// <inheritdoc />
        public ConcurrentQueue<LogEventArgs> LogQueue { get; } = new();

        /// <inheritdoc />
        public int MaxQueueSize { get; set; } = 10000;

        private BaseLoggerSink() { }

        /// <summary>
        /// Creates a new non-singleton instance of <see cref="BaseLoggerSink"/> for testing purposes.
        /// </summary>
        /// <returns>A new <see cref="BaseLoggerSink"/> instance.</returns>
        public static BaseLoggerSink CreateForTesting() => new();

        /// <inheritdoc />
        public void Write(LogEventArgs logEvent)
        {
            if (logEvent is null) return;

            // Skip if same instance already written (prevents duplicates from dual paths)
            if (!_recentEvents.TryAdd(logEvent, null))
                return;

            LogQueue.Enqueue(logEvent);
            int currentCount = Interlocked.Increment(ref _queueCount);

            // Only check trim when likely over limit (avoids Count property)
            if (currentCount > MaxQueueSize)
            {
                TrimQueueIfNeeded(currentCount);
            }

            // Fire event for UI subscribers
            _ = RaiseLogReceivedAsync(logEvent);
        }

        /// <summary>
        /// Trims the queue if it exceeds MaxQueueSize.
        /// Note: This is a best-effort trim - under high concurrency, the queue may
        /// temporarily exceed MaxQueueSize by a small amount. This is intentional
        /// to avoid locking overhead in the hot path.
        /// </summary>
        private void TrimQueueIfNeeded(int currentCount)
        {
            int overflow = currentCount - MaxQueueSize;
            if (overflow > 0)
            {
                // Remove overflow plus 10% buffer to reduce frequency of trimming
                int toRemove = overflow + (MaxQueueSize / 10);
                int removed = 0;
                for (int i = 0; i < toRemove && LogQueue.TryDequeue(out _); i++)
                    removed++;
                Interlocked.Add(ref _queueCount, -removed);
            }
        }

        private async Task RaiseLogReceivedAsync(LogEventArgs e)
        {
            var handler = LogReceived;
            if (handler is null) return;

            try
            {
                var handlers = handler.GetInvocationList();
                var length = handlers.Length;

                // Fast path: single subscriber (most common case)
                if (length == 1)
                {
                    try
                    {
                        await ((LogEvent)handlers[0]).Invoke(this, e).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[BaseLoggerSink] Handler exception: {ex}");
                    }
                    return;
                }

                // Multiple subscribers: use ArrayPool to avoid allocations
                var tasks = ArrayPool<Task>.Shared.Rent(length);
                try
                {
                    for (int i = 0; i < length; i++)
                    {
                        try
                        {
                            tasks[i] = ((LogEvent)handlers[i]).Invoke(this, e);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[BaseLoggerSink] Handler exception: {ex}");
                            tasks[i] = Task.CompletedTask;
                        }
                    }
                    await Task.WhenAll(new ArraySegment<Task>(tasks, 0, length)).ConfigureAwait(false);
                }
                finally
                {
                    ArrayPool<Task>.Shared.Return(tasks, clearArray: true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BaseLoggerSink] RaiseLogReceivedAsync exception: {ex}");
            }
        }
    }
}
