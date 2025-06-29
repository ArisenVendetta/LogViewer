using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LogViewer
{
    public abstract partial class BaseLogger
    {
        const string DefaultLogDateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff (zzz)";
        const string DefaultLogExportFormat = "{timestamp}|{loglevel}|{threadid}|{handle}|{message}";

        private static string _logExportFormat = DefaultLogExportFormat;

        /// <summary>
        /// Indicates whether the system has been initialized.
        /// </summary>
        internal static bool Initialized;
        /// <summary>
        /// Occurs when a debug log event is triggered.
        /// </summary>
        /// <remarks>This event is used to handle debug-level log messages. Subscribers can attach
        /// handlers  to process or respond to debug log events as needed.</remarks>
        internal static event LogEvent? DebugLogEvent;
        /// <summary>
        /// Gets the queue that stores debug log events for processing.
        /// </summary>
        internal static ConcurrentQueue<LogEventArgs>? DebugLogQueue { get; private set; }
        /// <summary>
        /// Handles log events by enqueuing them into the debug log queue.
        /// </summary>
        /// <remarks>This method enqueues the provided log event into the debug log queue. If the queue
        /// exceeds the maximum allowed size, the oldest entries are removed to maintain the size limit. The method is
        /// designed to run asynchronously.</remarks>
        /// <param name="sender">The source of the log event. This parameter is not used in the method.</param>
        /// <param name="e">The log event arguments containing the log data to be enqueued. Must not be <see langword="null"/>.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Gets the version of the assembly containing the <see cref="BaseLogger"/> class.
        /// </summary>
        public static Version Version { get; } = typeof(BaseLogger).Assembly.GetName().Version ?? new Version(1, 0, 0, 0);

        /// <summary>
        /// Initializes the logging system with the specified logger factory and configuration settings.
        /// </summary>
        /// <remarks>This method must be called before any logging operations are performed. It sets up
        /// the logging system, including initializing the log queue and event handlers. Subsequent calls to this method
        /// will have no effect if the logging system has already been initialized.</remarks>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> instance used to create loggers. This parameter cannot be <see
        /// langword="null"/>.</param>
        /// <param name="maxLogQueueSize">The maximum number of log entries that can be queued for processing. The default value is 10,000.</param>
        /// <param name="logDateTimeFormat">The date and time format string used for log entries. If not specified or empty, a default format of
        /// "yyyy-MM-dd HH:mm:ss.fff (zzz)" is used.</param>
        /// <param name="logExportFormat">The format used for exporting log entries. If not specified or empty, a default export format is used.</param>
        public static void Initialize(ILoggerFactory? loggerFactory, int maxLogQueueSize = 10000, string logDateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff (zzz)", string logExportFormat = DefaultLogExportFormat)
        {
            if (Initialized) return;
            ArgumentNullException.ThrowIfNull(loggerFactory, paramName: nameof(loggerFactory));
            LoggerFactory = loggerFactory;
            DebugLogQueue = new ConcurrentQueue<LogEventArgs>();
            DebugLogEvent += LogQueueHandlerAsync;
            Initialized = true;
            MaxLogQueueSize = maxLogQueueSize;
            LogDateTimeFormat = string.IsNullOrWhiteSpace(logDateTimeFormat) ? DefaultLogDateTimeFormat : logDateTimeFormat;
            LogExportFormat = string.IsNullOrWhiteSpace(logExportFormat) ? DefaultLogExportFormat : logExportFormat;
        }

        /// <summary>
        /// Gets or sets the <see cref="ILoggerFactory"/> instance used for creating loggers.
        /// </summary>
        /// <remarks>This property is typically used to provide a custom logger factory for logging
        /// purposes. It can be set internally to ensure consistent logging behavior across the application.</remarks>
        public static ILoggerFactory? LoggerFactory { get; internal set; }
        /// <summary>
        /// Gets or sets a value indicating whether log entries should include the current time in UTC format.
        /// </summary>
        /// <remarks>When enabled, all log entries will include a timestamp in Coordinated Universal Time
        /// (UTC), which can be useful for systems operating across multiple time zones.</remarks>
        public static bool LogUTCTime { get; set; }
        /// <summary>
        /// Gets or sets the format used for exporting log data.
        /// </summary>
        public static string LogExportFormat
        {
            get => _logExportFormat;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    _logExportFormat = DefaultLogExportFormat;
                else
                    _logExportFormat = value;
            }
        }
        /// <summary>
        /// Gets or sets the date and time format string used for logging.
        /// </summary>
        /// <remarks>The format string should follow the standard .NET date and time format patterns.  For
        /// example, "yyyy-MM-dd HH:mm:ss" can be used for a typical timestamp format.</remarks>
        public static string LogDateTimeFormat { get; set; }                           = DefaultLogDateTimeFormat;
        /// <summary>
        /// Gets or sets a value indicating whether timestamps should be included in the output.
        /// </summary>
        public static bool IncludeTimestampInOutput { get; set; }                      = true;
        /// <summary>
        /// Gets or sets the collection of characters to exclude from handle generation.
        /// </summary>
        public static IReadOnlyCollection<char> ExcludeCharsFromHandle { get; set; }   = ['.', '-'];
        /// <summary>
        /// Gets or sets the maximum size of the log queue.
        /// </summary>
        /// <remarks>If the log queue exceeds this size, the oldest log entries will be removed to make
        /// room for new ones. Adjust this value based on the application's logging requirements and available
        /// memory.</remarks>
        public static int MaxLogQueueSize { get; set; }                                = 10000;
        /// <summary>
        /// Gets the collection of file types supported for export operations.
        /// </summary>
        /// <remarks>This property provides a predefined list of file types that can be used for exporting
        /// data.  The collection includes common formats such as text files, CSV files, and JSON files.</remarks>
        public static ObservableCollection<FileType> SupportedExportFileTypes { get; } = [
            new("Text file", ".txt"),
            new("CSV file", ".csv"),
            new("JSON file", ".json")
        ];
    }
}
