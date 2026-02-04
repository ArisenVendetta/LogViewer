using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.Extensions.Logging;

namespace LogViewer
{
    public abstract partial class BaseLogger
    {
        internal const string DefaultLogDateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff (zzz)";
        internal const string DefaultLogExportFormat = "{timestamp}|{loglevel}|{threadid}|{handle}|{message}";
        internal const string LogDisplayFormatFallback = "{timestamp} [{handle}] {message}";

        /// <summary>
        /// Represents the resource key for the <see cref="Converters.ColorToBrushConverter"/>.
        /// </summary>
        /// <remarks>This constant can be used to reference the <see cref="Converters.ColorToBrushConverter"/> in
        /// resource dictionaries or when retrieving the converter from a resource collection.</remarks>
        public const string ColorToBrushConverterKey = nameof(ColorToBrushConverterKey);
        /// <summary>
        /// Represents the resource key for the <see cref="Converters.InverseBooleanConverter"/>.
        /// </summary>
        /// <remarks>This key is used to reference the <see cref="Converters.InverseBooleanConverter"/> that inverts a boolean value
        /// in data binding scenarios where a true value needs to be displayed as false, and vice
        /// versa.</remarks>
        public const string InverseBooleanConverterKey = nameof(InverseBooleanConverterKey);
        /// <summary>
        /// Represents the resource key for the <see cref="Converters.LogLevelColorConverter"/>.
        /// </summary>
        /// <remarks>This constant can be used to reference the <see cref="Converters.LogLevelColorConverter"/> in
        /// resource dictionaries or when retrieving the converter from a resource collection.</remarks>
        public const string LogLevelToBrushConverterKey = nameof(LogLevelToBrushConverterKey);
        /// <summary>
        /// Represents the resource key for the BooleanToVisibilityConverter.
        /// </summary>
        /// <remarks>This key can be used to reference the BooleanToVisibilityConverter in resource
        /// dictionaries or other contexts where a resource key is required.</remarks>
        public const string BooleanToVisibilityConverterKey = nameof(BooleanToVisibilityConverterKey);

        private static string _logExportFormat = DefaultLogExportFormat;
        private static string _defaultLogDisplayFormat = LogDisplayFormatFallback;

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
            if (e is null) return;
            if (DebugLogQueue is null) return;
            DebugLogQueue.Enqueue(e);
            int overflow = DebugLogQueue.Count - MaxLogQueueSize;
            if (overflow > 0)
            {
                // Remove overflow plus 10% buffer to reduce frequency of trimming
                int toRemove = overflow + (MaxLogQueueSize / 10);
                for (int i = 0; i < toRemove && DebugLogQueue.TryDequeue(out _); i++) { }
            }
            await Task.CompletedTask;
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
        /// Shuts down the logging system and releases associated resources.
        /// </summary>
        /// <remarks>After calling this method, the logging system is no longer initialized and cannot be
        /// used until reinitialized. Any pending log events will be discarded.</remarks>
        public static void Shutdown()
        {
            if (!Initialized) return;
            DebugLogEvent -= LogQueueHandlerAsync;
            DebugLogQueue = null;
            LoggerFactory = null;
            Initialized = false;
        }

        internal static string SanitizeHandle(string name)
        {
            string sanitizedHandle = name ?? string.Empty;
            var excludeSet = ExcludeCharsFromHandle?.ToHashSet() ?? [];
            excludeSet.Add(' ');
            return new string(sanitizedHandle.Where(c => !excludeSet.Contains(c)).ToArray()).Trim();
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
        /// Gets or sets the default format string used for displaying log messages.
        /// </summary>
        /// <remarks>The format string determines how log messages are structured when displayed.  If the
        /// value is set to <see langword="null"/> or whitespace, the fallback format is applied.</remarks>
        public static string DefaultLogDisplayFormat
        {
            get => _defaultLogDisplayFormat ??= LogDisplayFormatFallback;
            set
            {
                if (_defaultLogDisplayFormat == value) return;
                _defaultLogDisplayFormat = string.IsNullOrWhiteSpace(value) ? LogDisplayFormatFallback : value;
            }
        }
        /// <summary>
        /// Gets or sets the date and time format string used for logging.
        /// </summary>
        /// <remarks>The format string should follow the standard .NET date and time format patterns.  For
        /// example, "yyyy-MM-dd HH:mm:ss" can be used for a typical timestamp format.</remarks>
        public static string LogDateTimeFormat { get; set; } = DefaultLogDateTimeFormat;
        /// <summary>
        /// Gets or sets a value indicating whether timestamps should be included in the output.
        /// </summary>
        public static bool IncludeTimestampInOutput { get; set; } = true;
        /// <summary>
        /// Gets or sets the collection of characters to exclude from handle generation.
        /// </summary>
        public static IReadOnlyCollection<char> ExcludeCharsFromHandle { get; set; } = ['.', '-'];
        /// <summary>
        /// Gets or sets the maximum size of the log queue.
        /// </summary>
        /// <remarks>If the log queue exceeds this size, the oldest log entries will be removed to make
        /// room for new ones. Adjust this value based on the application's logging requirements and available
        /// memory.</remarks>
        public static int MaxLogQueueSize { get; set; } = 10000;
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

        /// <summary>
        /// Creates a new logger instance with the specified handle, color, and log level.
        /// </summary>
        /// <param name="handle">The unique identifier for the logger. If <see langword="null"/>, a default handle is used.</param>
        /// <param name="color">The color associated with the logger's output. If <see langword="null"/>, a default color is used.</param>
        /// <param name="logLevel">The minimum log level for the logger. Defaults to <see cref="LogLevel.Information"/>.</param>
        /// <returns>An <see cref="ILoggable"/> instance configured with the specified parameters.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the logger factory is not initialized. Ensure <c>Initialize()</c> is called before creating a
        /// logger.</exception>
        public static ILogger CreateLogger(string? handle = null, Color? color = null, LogLevel logLevel = LogLevel.Information)
        {
            if (LoggerFactory is null)
                throw new InvalidOperationException("LoggerFactory is not initialized. Call Initialize() before creating a logger.");
            return new Logger(handle, color, logLevel);
        }

        /// <summary>
        /// Creates a logger instance for the specified type with optional color and log level settings.
        /// </summary>
        /// <typeparam name="T">The type for which the logger is being created. The logger will use the name of this type.</typeparam>
        /// <param name="color">An optional color to associate with the logger output. If not specified, the default color is used.</param>
        /// <param name="logLevel">The minimum log level for the logger. Defaults to <see cref="LogLevel.Information"/>.</param>
        /// <returns>An <see cref="ILoggable"/> instance configured for the specified type.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the logger factory is not initialized. Ensure <c>Initialize()</c> is called before creating a
        /// logger.</exception>
        public static ILogger CreateLogger<T>(Color? color = null, LogLevel logLevel = LogLevel.Information)
        {
            if (LoggerFactory is null)
                throw new InvalidOperationException("LoggerFactory is not initialized. Call Initialize() before creating a logger.");
            return new Logger(typeof(T).Name, color, logLevel);
        }
    }
}
