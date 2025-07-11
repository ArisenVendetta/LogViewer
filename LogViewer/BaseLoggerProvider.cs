using System.Collections.Concurrent;
using System.Windows.Media;
using Microsoft.Extensions.Logging;

namespace LogViewer
{
    /// <summary>
    /// Provides a base implementation for a logger provider that supports category-based logging with customizable
    /// colors.
    /// </summary>
    /// <remarks>This class implements the <see cref="ILoggerProvider"/> interface, allowing for the creation
    /// of loggers associated with specific category names. It supports setting colors for categories to enable visual
    /// differentiation in log outputs. The default log level can be specified during instantiation.</remarks>
    /// <param name="defaultLogLevel"></param>
    public class BaseLoggerProvider(LogLevel defaultLogLevel = LogLevel.Trace) : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, Color> _categoryColors = [];

        /// <summary>
        /// Gets or sets the default log level for the logger.
        /// </summary>
        public LogLevel DefaultLogLevel { get; set; } = defaultLogLevel;

        /// <summary>
        /// Creates a logger for the specified category name.
        /// </summary>
        /// <remarks>The logger is created with a color associated with the category name, if available;
        /// otherwise, it defaults to black. The default log level is applied to the logger.</remarks>
        /// <param name="categoryName">The name of the category for which the logger is created. This value cannot be null or empty.</param>
        /// <returns>An <see cref="ILogger"/> instance configured for the specified category name.</returns>
        public ILogger CreateLogger(string categoryName) => CreateLogger(categoryName, null, null);

        /// <summary>
        /// Creates a logger instance for the specified type.
        /// </summary>
        /// <typeparam name="T">The type for which the logger is created.</typeparam>
        /// <param name="color">An optional color to use for log messages. If not specified, a default color is used.</param>
        /// <param name="logLevel">An optional log level to set for the logger. If not specified, the default log level is used.</param>
        /// <returns>An <see cref="ILogger"/> instance configured for the specified type.</returns>
        public ILogger CreateLogger<T>(Color? color = null, LogLevel? logLevel = null) => CreateLogger(typeof(T).Name, color, logLevel);

        /// <summary>
        /// Creates a new logger instance with the specified category name, color, and log level.
        /// </summary>
        /// <param name="categoryName">The category name for the logger, used to group log messages.</param>
        /// <param name="color">The optional color to associate with the logger. Defaults to black if not specified.</param>
        /// <param name="logLevel">The optional log level to set for the logger. Defaults to the predefined default log level if not specified.</param>
        /// <returns>An <see cref="ILogger"/> instance configured with the specified category name, color, and log level.</returns>
        public ILogger CreateLogger(string categoryName, Color? color = null, LogLevel? logLevel = null)
        {
            try
            {
                categoryName = BaseLogger.SanitizeHandle(categoryName);
                color ??= _categoryColors.TryGetValue(categoryName, out Color foundColor) ? foundColor : Colors.Black;
                logLevel ??= DefaultLogLevel;
                return new Logger(categoryName, color, logLevel.Value);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create logger for category '{categoryName}'", ex);
            }
        }

        /// <summary>
        /// Sets the color associated with a specified category name.
        /// </summary>
        /// <param name="categoryName">The name of the category for which to set the color. Cannot be null or empty.</param>
        /// <param name="color">The <see cref="Color"/> to associate with the specified category.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="categoryName"/> is null or empty.</exception>
        public void SetCategoryColor(string categoryName, Color color)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(categoryName))
                {
                    throw new ArgumentException("Category name cannot be null or empty", nameof(categoryName));
                }
                categoryName = BaseLogger.SanitizeHandle(categoryName);
                _categoryColors[categoryName] = color;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set color for category '{categoryName}'", ex);
            }
        }

        /// <summary>
        /// Sets the color associated with a specific log category.
        /// </summary>
        /// <remarks>This method assigns a color to a log category, which can be used for visual
        /// differentiation in log outputs. The category is determined by the type parameter <typeparamref
        /// name="T"/>.</remarks>
        /// <typeparam name="T">The type representing the log category.</typeparam>
        /// <param name="color">The <see cref="Color"/> to associate with the specified log category.</param>
        public void SetCategoryColor<T>(Color color)
        {
            try
            {
                string name = BaseLogger.SanitizeHandle(typeof(T).Name);
                _categoryColors[name] = color;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set color for category '{typeof(T).Name}'", ex);
            }
        }

        /// <summary>
        /// Sets the color for each category specified in the provided dictionary.
        /// </summary>
        /// <remarks>The method updates the internal category color mapping with the colors specified in
        /// <paramref name="colorMap"/>. Category names are sanitized before being used.</remarks>
        /// <param name="colorMap">A read-only dictionary mapping category names to their corresponding colors.  If <paramref name="colorMap"/>
        /// is <see langword="null"/>, an empty dictionary is used.</param>
        /// <exception cref="InvalidOperationException">Thrown if setting a color for any category fails.</exception>
        public void SetCategoryColor(IReadOnlyDictionary<string, Color> colorMap)
        {
            colorMap ??= new Dictionary<string, Color>();
            try
            {
                foreach (var kvp in colorMap)
                {
                    string categoryName = BaseLogger.SanitizeHandle(kvp.Key);
                    try
                    {
                        _categoryColors[categoryName] = kvp.Value;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Failed to set color for category '{categoryName}'", ex);
                    }
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to set category colors from the provided dictionary", ex);
            }
        }

        /// <summary>
        /// Releases all resources used by the current instance of the class.
        /// </summary>
        /// <remarks>This method is provided to comply with the <see cref="IDisposable"/> interface. It
        /// does not perform any resource cleanup as there are no resources to dispose.</remarks>
        public void Dispose()
        {
            // nothing to dispose of
            GC.SuppressFinalize(this);
        }
    }
}
