using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LogViewer
{
    public abstract partial class BaseLogger
    {
        internal static bool Initialized;
        public static void Initialize(ILoggerFactory? loggerFactory)
        {
            if (Initialized) return;
            ArgumentNullException.ThrowIfNull(loggerFactory, paramName: nameof(loggerFactory));
            LoggerFactory = loggerFactory;
            DebugLogQueue = new ConcurrentQueue<LogEventArgs>();
            DebugLogEvent += LogQueueHandlerAsync;
            Initialized = true;
        }

        public static ILoggerFactory? LoggerFactory { get; internal set; }
        internal static event LogEvent? DebugLogEvent;
        internal static ConcurrentQueue<LogEventArgs>? DebugLogQueue { get; private set; }
        internal static async Task LogQueueHandlerAsync(object sender, LogEventArgs e)
        {
            await Task.Run(() =>
            {
                if (e is null) return;
                if (DebugLogQueue is null) return;
                DebugLogQueue.Enqueue(e);
                while (DebugLogQueue.Count > MaxLogQueueSize)
                {
                    _ = DebugLogQueue.TryDequeue(out _);
                }
            });
        }

        public static Version Version { get; } = typeof(BaseLogger).Assembly.GetName().Version ?? new Version(1, 0, 0, 0);

        public static string LogDateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff (zzz)";
        public static bool LogUTCTime { get; set; }
        public static bool IncludeTimestampInOutput { get; set; } = true;
        public static IReadOnlyCollection<char> ExcludeCharsFromName { get; set; } = ['.', '-'];
        public static int MaxLogQueueSize { get; set; } = 10000;
    }
}
