using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.Extensions.Logging;

namespace LogViewer
{
    /// <summary>
    /// Defines a contract for log-capable objects that support structured, color-coded, and event-driven logging.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface can log messages at various levels, raise log events, and provide metadata such as handle and color.
    /// </remarks>
    public interface ILoggable
    {
        /// <summary>
        /// Gets the handle (name) for this logger instance.
        /// </summary>
        string LogHandle { get; }
        /// <summary>
        /// Gets or sets the color associated with this logger instance.
        /// </summary>
        Color LogColor { get; set; }

        /// <summary>
        /// Gets the underlying <see cref="ILogger"/> instance used for logging.
        /// </summary>
        ILogger Logger { get; }
        /// <summary>
        /// Gets the minimum <see cref="LogLevel"/> for this logger instance.
        /// </summary>
        /// <remarks>
        /// Not Implemented yet - Intent is to use this property to filter it's own log messages in the viewer. For example, if this base logger is set to <see cref="LogLevel.Warning"/>, we don't want to see the messages but we still want them logged.
        /// </remarks>
        LogLevel LogLevel { get; }

        /// <summary>
        /// Occurs when a log event is raised, allowing subscribers to receive log messages in real time.
        /// </summary>
        event LogEvent? LogEvent;

        /// <summary>
        /// Logs a message at the specified <see cref="LogLevel"/>.
        /// </summary>
        /// <param name="level">The severity level of the log message.</param>
        /// <param name="message">The message to log.</param>
        void Log(LogLevel level, string message);
        /// <summary>
        /// Logs each item in an enumerable collection at the specified <see cref="LogLevel"/>.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="level">The severity level of the log message.</param>
        /// <param name="iterable">The collection to log.</param>
        void Log<T>(LogLevel level, IEnumerable<T> iterable);
        /// <summary>
        /// Logs each key-value pair in a dictionary at the specified <see cref="LogLevel"/>.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
        /// <param name="level">The severity level of the log message.</param>
        /// <param name="dict">The dictionary to log.</param>
        void Log<TKey, TValue>(LogLevel level, IDictionary<TKey, TValue> dict);

        /// <summary>
        /// Logs a message at the <see cref="LogLevel.Information"/> level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void LogInfo(string message);
        /// <summary>
        /// Logs each item in an enumerable collection at the <see cref="LogLevel.Information"/> level.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="iterable">The collection to log.</param>
        void LogInfo<T>(IEnumerable<T> iterable);
        /// <summary>
        /// Logs each key-value pair in a dictionary at the <see cref="LogLevel.Information"/> level.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
        /// <param name="dict">The dictionary to log.</param>
        void LogInfo<TKey, TValue>(IDictionary<TKey, TValue> dict);

        /// <summary>
        /// Logs a message at the <see cref="LogLevel.Debug"/> level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void LogDebug(string message);
        /// <summary>
        /// Logs each item in an enumerable collection at the <see cref="LogLevel.Debug"/> level.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="iterable">The collection to log.</param>
        void LogDebug<T>(IEnumerable<T> iterable);
        /// <summary>
        /// Logs each key-value pair in a dictionary at the <see cref="LogLevel.Debug"/> level.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
        /// <param name="dict">The dictionary to log.</param>
        void LogDebug<TKey, TValue>(IDictionary<TKey, TValue> dict);

        /// <summary>
        /// Logs a message at the <see cref="LogLevel.Critical"/> level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void LogCritical(string message);
        /// <summary>
        /// Logs each item in an enumerable collection at the <see cref="LogLevel.Critical"/> level.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="iterable">The collection to log.</param>
        void LogCritical<T>(IEnumerable<T> iterable);
        /// <summary>
        /// Logs each key-value pair in a dictionary at the <see cref="LogLevel.Critical"/> level.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
        /// <param name="dict">The dictionary to log.</param>
        void LogCritical<TKey, TValue>(IDictionary<TKey, TValue> dict);

        /// <summary>
        /// Logs a message at the <see cref="LogLevel.Warning"/> level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void LogWarning(string message);
        /// <summary>
        /// Logs each item in an enumerable collection at the <see cref="LogLevel.Warning"/> level.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="iterable">The collection to log.</param>
        void LogWarning<T>(IEnumerable<T> iterable);
        /// <summary>
        /// Logs each key-value pair in a dictionary at the <see cref="LogLevel.Warning"/> level.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
        /// <param name="dict">The dictionary to log.</param>
        void LogWarning<TKey, TValue>(IDictionary<TKey, TValue> dict);

        /// <summary>
        /// Logs a message at the <see cref="LogLevel.Error"/> level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void LogError(string message);
        /// <summary>
        /// Logs each item in an enumerable collection at the <see cref="LogLevel.Error"/> level.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="iterable">The collection to log.</param>
        void LogError<T>(IEnumerable<T> iterable);
        /// <summary>
        /// Logs each key-value pair in a dictionary at the <see cref="LogLevel.Error"/> level.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
        /// <param name="dict">The dictionary to log.</param>
        void LogError<TKey, TValue>(IDictionary<TKey, TValue> dict);

        /// <summary>
        /// Logs a message at the <see cref="LogLevel.Trace"/> level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void LogTrace(string message);
        /// <summary>
        /// Logs each item in an enumerable collection at the <see cref="LogLevel.Trace"/> level.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="iterable">The collection to log.</param>
        void LogTrace<T>(IEnumerable<T> iterable);
        /// <summary>
        /// Logs each key-value pair in a dictionary at the <see cref="LogLevel.Trace"/> level.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
        /// <param name="dict">The dictionary to log.</param>
        void LogTrace<TKey, TValue>(IDictionary<TKey, TValue> dict);

        /// <summary>
        /// Logs an exception with an optional header message and log level.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="headerMessage">A header message to prefix the exception details.</param>
        /// <param name="logLevel">The log level to use for the exception. Defaults to <see cref="LogLevel.Error"/>.</param>
        void LogException(Exception exception, string headerMessage, LogLevel logLevel = LogLevel.Error);
        /// <summary>
        /// Logs an exception at the <see cref="LogLevel.Error"/> level with no header message.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        void LogException(Exception exception);

        /// <summary>
        /// Subscribes a synchronous handler to the <see cref="LogEvent"/> event.
        /// </summary>
        /// <param name="handler">The handler to invoke when a log event is raised.</param>
        /// <returns>
        /// A <see cref="LogEvent"/> delegate that can be used to unsubscribe, or <c>null</c> if the handler is <c>null</c>.
        /// </returns>
        LogEvent? SubscribeLogEventSync(Action<object, LogEventArgs> handler);
    }
}
