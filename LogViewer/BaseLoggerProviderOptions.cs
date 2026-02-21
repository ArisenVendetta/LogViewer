using System.Windows.Media;
using Microsoft.Extensions.Logging;

namespace LogViewer
{
    /// <summary>
    /// Configuration options for LogViewer.
    /// </summary>
    public class BaseLoggerProviderOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of log entries to keep in the queue.
        /// Default is 10000.
        /// </summary>
        public int MaxQueueSize { get; set; } = 10000;

        /// <summary>
        /// Gets or sets the minimum log level for LogViewer to process.
        /// Default is <see cref="LogLevel.Trace"/>.
        /// </summary>
        public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

        /// <summary>
        /// Gets a dictionary mapping category names to colors for visual differentiation.
        /// </summary>
        public Dictionary<string, Color> CategoryColors { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets the date/time format string used for log timestamps.
        /// Default is "yyyy-MM-dd HH:mm:ss.fff (zzz)".
        /// </summary>
        public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff (zzz)";

        /// <summary>
        /// Gets or sets whether to use UTC time for log timestamps.
        /// Default is false (local time).
        /// </summary>
        public bool UseUtcTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the namespace is removed from the category name.
        /// </summary>
        /// <remarks>When set to <see langword="true"/>, the namespace portion of the category name is
        /// omitted, which can improve readability in user interfaces or logs by displaying only the category's simple
        /// name.</remarks>
        public bool StripNamespaceFromCategory { get; set; } = true;
    }
}
