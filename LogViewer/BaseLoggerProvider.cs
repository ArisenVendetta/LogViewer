using System.Collections.Concurrent;
using System.Windows.Media;
using Microsoft.Extensions.Logging;

namespace LogViewer
{
    /// <summary>
    /// ILoggerProvider implementation that creates <see cref="BaseLogger"/> instances for the DI pattern.
    /// Can optionally wrap another <see cref="ILoggerFactory"/> to pass logs through to other providers.
    /// </summary>
    /// <remarks>
    /// Creates a <see cref="BaseLoggerProvider"/> with a custom sink.
    /// </remarks>
    /// <param name="sink">The sink to write log events to.</param>
    /// <param name="innerFactory">Optional inner factory to pass through log calls to.</param>
    public sealed class BaseLoggerProvider(IBaseLoggerSink sink, ILoggerFactory? innerFactory = null) : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, BaseLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, Color> _categoryColors = new(StringComparer.OrdinalIgnoreCase);
        private readonly IBaseLoggerSink _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        private readonly ILoggerFactory? _innerFactory = innerFactory;
        private bool _disposed;

        /// <summary>
        /// Gets or sets the minimum log level for loggers created by this provider.
        /// </summary>
        public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

        /// <summary>
        /// Gets or sets a value indicating whether the namespace is removed from the category name.
        /// </summary>
        /// <remarks>When set to <see langword="true"/>, the namespace portion of the category name is
        /// omitted, which can improve readability in user interfaces or logs by displaying only the simple category
        /// name.</remarks>
        public bool StripNamespaceFromCategory { get; set; } = true;

        /// <summary>
        /// Creates a <see cref="BaseLoggerProvider"/> with the default singleton sink.
        /// </summary>
        public BaseLoggerProvider()
            : this(BaseLoggerSink.Instance, null) { }

        /// <summary>
        /// Creates a <see cref="BaseLoggerProvider"/> that wraps an existing <see cref="ILoggerFactory"/>.
        /// Logs will be sent to both LogViewer and the wrapped factory.
        /// </summary>
        /// <param name="innerFactory">The factory to wrap (NLog, Serilog, etc.).</param>
        public BaseLoggerProvider(ILoggerFactory innerFactory)
            : this(BaseLoggerSink.Instance, innerFactory) { }

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name =>
            {
                var typeName = name;
                if (StripNamespaceFromCategory)
                {
                    // Extract just the type name if this is a fully-qualified name (e.g., "Namespace.TypeName" -> "TypeName")
                    var lastDotIndex = name.LastIndexOf('.');
                    if (lastDotIndex >= 0 && lastDotIndex < name.Length - 1)
                    {
                        typeName = name[(lastDotIndex + 1)..];
                    }
                }

                var sanitizedName = BaseLogger.SanitizeHandle(typeName);
                var color = _categoryColors.TryGetValue(sanitizedName, out var c) ? c : Colors.Black;
                var innerLogger = _innerFactory?.CreateLogger(name);
                return new BaseLogger(sanitizedName, color, _sink, innerLogger, this);
            });
        }

        /// <summary>
        /// Sets the color associated with a specified category name.
        /// </summary>
        /// <param name="categoryName">The category name.</param>
        /// <param name="color">The color to associate with the category.</param>
        public void SetCategoryColor(string categoryName, Color color)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                throw new ArgumentException("Category name cannot be null or empty", nameof(categoryName));

            _categoryColors[BaseLogger.SanitizeHandle(categoryName)] = color;
        }

        /// <summary>
        /// Sets the color associated with a specific type's category.
        /// </summary>
        /// <typeparam name="T">The type whose name will be used as the category.</typeparam>
        /// <param name="color">The color to associate with the category.</param>
        public void SetCategoryColor<T>(Color color)
        {
            SetCategoryColor(typeof(T).Name, color);
        }

        /// <summary>
        /// Sets colors for multiple categories at once.
        /// </summary>
        /// <param name="colorMap">A dictionary mapping category names to colors.</param>
        public void SetCategoryColors(IReadOnlyDictionary<string, Color> colorMap)
        {
            if (colorMap is null) return;

            foreach (var kvp in colorMap)
            {
                SetCategoryColor(kvp.Key, kvp.Value);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            _loggers.Clear();
            _disposed = true;
        }
    }
}
