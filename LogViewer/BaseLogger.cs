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
    /// Provides a base class for logging functionality, routing all log messages to a central real-time log viewer.
    /// Implements <see cref="ILoggable"/> and supports structured, color-coded, and event-driven logging.
    /// </summary>
    public partial class BaseLogger : ILoggable, ILogger
    {
        /// <summary>
        /// Predefined logging actions for trace-level messages.
        /// </summary>
        internal static readonly Action<ILogger, string, Exception?> LogTraceMessage = LoggerMessage.Define<string>(LogLevel.Trace, new EventId(0, nameof(LogTrace)), "{Message}");
        /// <summary>
        /// Predefined logging actions for debug-level messages.
        /// </summary>
        internal static readonly Action<ILogger, string, Exception?> LogDebugMessage = LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1, nameof(LogDebug)), "{Message}");
        /// <summary>
        /// Predefined logging actions for information-level messages.
        /// </summary>
        internal static readonly Action<ILogger, string, Exception?> LogInfoMessage = LoggerMessage.Define<string>(LogLevel.Information, new EventId(2, nameof(LogInformation)), "{Message}");
        /// <summary>
        /// Predefined logging actions for warning-level messages.
        /// </summary>
        internal static readonly Action<ILogger, string, Exception?> LogWarningMessage = LoggerMessage.Define<string>(LogLevel.Warning, new EventId(3, nameof(LogWarning)), "{Message}");
        /// <summary>
        /// Predefined logging actions for error-level messages.
        /// </summary>
        internal static readonly Action<ILogger, string, Exception?> LogErrorMessage = LoggerMessage.Define<string>(LogLevel.Error, new EventId(4, nameof(LogError)), "{Message}");
        /// <summary>
        /// Predefined logging actions for critical-level messages.
        /// </summary>
        internal static readonly Action<ILogger, string, Exception?> LogCriticalMessage = LoggerMessage.Define<string>(LogLevel.Critical, new EventId(5, nameof(LogCritical)), "{Message}");

        /// <summary>
        /// Predefined logging actions for trace-level exceptions.
        /// </summary>
        internal static readonly Action<ILogger, string, Exception?> LogTraceException = LoggerMessage.Define<string>(LogLevel.Trace, new EventId(100, nameof(LogTrace)), "{Message}");
        /// <summary>
        /// Predefined logging actions for debug-level exceptions.
        /// </summary>
        internal static readonly Action<ILogger, string, Exception?> LogDebugException = LoggerMessage.Define<string>(LogLevel.Debug, new EventId(101, nameof(LogDebug)), "{Message}");
        /// <summary>
        /// Predefined logging actions for information-level exceptions.
        /// </summary>
        internal static readonly Action<ILogger, string, Exception?> LogInfoException = LoggerMessage.Define<string>(LogLevel.Information, new EventId(102, nameof(LogInformation)), "{Message}");
        /// <summary>
        /// Predefined logging actions for warning-level exceptions.
        /// </summary>
        internal static readonly Action<ILogger, string, Exception?> LogWarningException = LoggerMessage.Define<string>(LogLevel.Warning, new EventId(103, nameof(LogWarning)), "{Message}");
        /// <summary>
        /// Predefined logging actions for error-level exceptions.
        /// </summary>
        internal static readonly Action<ILogger, string, Exception?> LogErrorException = LoggerMessage.Define<string>(LogLevel.Error, new EventId(104, nameof(LogError)), "{Message}");
        /// <summary>
        /// Predefined logging actions for critical-level exceptions.
        /// </summary>
        internal static readonly Action<ILogger, string, Exception?> LogCriticalException = LoggerMessage.Define<string>(LogLevel.Critical, new EventId(105, nameof(LogCritical)), "{Message}");

        /// <summary>
        /// Maps <see cref="LogLevel"/> values to their corresponding log message actions.
        /// </summary>
        internal static readonly Dictionary<LogLevel, Action<ILogger, string, Exception?>> LogActions = new()
        {
            { LogLevel.Trace, LogTraceMessage },
            { LogLevel.Debug, LogDebugMessage },
            { LogLevel.Information, LogInfoMessage },
            { LogLevel.Warning, LogWarningMessage },
            { LogLevel.Error, LogErrorMessage },
            { LogLevel.Critical, LogCriticalMessage }
        };
        /// <summary>
        /// Maps <see cref="LogLevel"/> values to their corresponding exception log actions.
        /// </summary>
        internal static readonly Dictionary<LogLevel, Action<ILogger, string, Exception?>> LogExceptionActions = new()
        {
            { LogLevel.Trace, LogTraceException },
            { LogLevel.Debug, LogDebugException },
            { LogLevel.Information, LogInfoException },
            { LogLevel.Warning, LogWarningException },
            { LogLevel.Error, LogErrorException },
            { LogLevel.Critical, LogCriticalException }
        };

        private readonly IBaseLoggerSink? _sink;
        private readonly BaseLoggerProvider? _provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseLogger"/> class.
        /// </summary>
        /// <param name="handle">Optional log handle (name) for this logger. If null, the type name is used.</param>
        /// <param name="color">Optional color to associate with this logger. Defaults to black.</param>
        /// <param name="logLevel">The minimum <see cref="LogLevel"/> for this logger. Defaults to Information.</param>
        /// <exception cref="InvalidOperationException">Thrown if <c>LoggerFactory</c> is not initialized before instantiation.</exception>
        protected BaseLogger(string? handle = null, Color? color = null, LogLevel logLevel = LogLevel.Information)
        {
            if (LoggerFactory is null) throw new InvalidOperationException($"Must call {nameof(BaseLogger)}.{nameof(Initialize)} before creating an instance of {nameof(BaseLogger)}");

            // if for some reason this was set to null because someone wanted to not exclude anything, we reset it to an empty array which means no characters are excluded
            ExcludeCharsFromHandle ??= [];

            // sanitize the handle by removing unwanted characters and trimming whitespace
            // consumers of the code should be setting any characters they want to exclude in the static property BaseLogger.ExcludeCharsFromHandle
            LogHandle = SanitizeHandle(handle ?? GetType().Name);
            LogColor = color ?? Colors.Black;
            LogLevel = logLevel;

            // create the logger using the sanitized handle
            Logger = LoggerFactory.CreateLogger(LogHandle);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseLogger"/> class for the DI pattern.
        /// Used by <see cref="BaseLoggerProvider"/> to create logger instances.
        /// </summary>
        /// <param name="categoryName">The category name for this logger.</param>
        /// <param name="color">The color to associate with log entries from this logger.</param>
        /// <param name="sink">The sink to write log events to.</param>
        /// <param name="innerLogger">Optional inner logger to pass through log calls to.</param>
        /// <param name="provider">The provider that created this logger.</param>
        internal BaseLogger(
            string categoryName,
            Color color,
            IBaseLoggerSink sink,
            ILogger? innerLogger,
            BaseLoggerProvider provider)
        {
            ArgumentNullException.ThrowIfNull(sink);
            ArgumentNullException.ThrowIfNull(provider);

            _sink = sink;
            _provider = provider;

            // Dual sink write prevention is handled in Log() and LogException():
            // if Logger is BaseLogger, we skip _sink.Write() since the inner logger handles it
            Logger = innerLogger;

            ExcludeCharsFromHandle ??= [];
            LogHandle = SanitizeHandle(categoryName ?? string.Empty);
            LogColor = color;
            LogLevel = provider.MinimumLevel;
        }

        /// <summary>
        /// Gets the handle (name) for this logger instance.
        /// </summary>
        public string LogHandle { get; }
        /// <summary>
        /// Gets or sets the color associated with this logger instance.
        /// </summary>
        public Color LogColor { get; set; }
        /// <summary>
        /// Gets the underlying <see cref="ILogger"/> instance used for logging.
        /// May be null in DI mode when no inner logger is provided.
        /// </summary>
        public ILogger? Logger { get; }
        /// <summary>
        /// Gets or sets the current log level for the logger.
        /// </summary>
        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Occurs when a log event is raised, allowing subscribers to receive log messages in real time.
        /// </summary>
        public event LogEvent? LogEvent;

        /// <summary>
        /// Logs a message at the specified <see cref="LogLevel"/>.
        /// </summary>
        /// <param name="level">The severity level of the log message.</param>
        /// <param name="message">The message to log.</param>
        public void Log(LogLevel level, string message)
        {
            if (message is null) return;

            var args = new LogEventArgs(level, LogHandle, message, LogColor)
            {
                LogDateTime = LogUTCTime ? DateTime.UtcNow : DateTime.Now
            };

            // Write to sink if we have one (DI mode)
            if (Logger is not BaseLogger)
                _sink?.Write(args);

            // Pass through to Logger if we have one
            if (Logger != null)
            {
                if (!LogActions.TryGetValue(level, out var logAction))
                    logAction = LogInfoMessage;
                logAction(Logger, args.LogText, null);
            }

            OnLogEvent(args);
        }

        /// <summary>
        /// Logs each item in an enumerable collection at the specified <see cref="LogLevel"/>.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="level">The severity level of the log message.</param>
        /// <param name="iterable">The collection to log.</param>
        public void Log<T>(LogLevel level, IEnumerable<T> iterable)
        {
            if (iterable is null) return;
            try
            {
                Log(level, $"{typeof(T).Name}[]");
                Log(level, "{");
                uint counter = 0;
                foreach (var item in iterable)
                {
                    Log(level, $"\t[{counter++}] => {item?.ToString() ?? "null"}");
                }
                Log(level, "}");
            }
            catch (Exception ex)
            {
                if (Logger != null) LogErrorException(Logger, $"Error when attempting to log iterable of type: {typeof(T).Name}", ex);
            }
        }

        /// <summary>
        /// Logs each key-value pair in a dictionary at the specified <see cref="LogLevel"/>.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
        /// <param name="level">The severity level of the log message.</param>
        /// <param name="dict">The dictionary to log.</param>
        public void Log<TKey, TValue>(LogLevel level, IDictionary<TKey, TValue> dict)
        {
            if (dict is null) return;
            try
            {
                Log(level, $"Dict<{typeof(TKey).Name}, {typeof(TValue).Name}>");
                Log(level, "{");
                foreach (var item in dict)
                {
                    Log(level, $"\t[{item.Key?.ToString() ?? "null"}] => {item.Value?.ToString() ?? "null"}");
                }
                Log(level, "}");
            }
            catch (Exception ex)
            {
                if (Logger != null) LogErrorException(Logger, $"Error when attempting to log dictionary of types - Key: {typeof(TKey).Name}, Value: {typeof(TValue).Name}", ex);
            }
        }

        /// <summary>
        /// Logs a message at the <see cref="LogLevel.Critical"/> level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogCritical(string message) => Log(LogLevel.Critical, message);
        /// <summary>
        /// Logs each item in an enumerable collection at the <see cref="LogLevel.Critical"/> level.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="iterable">The collection to log.</param>
        public void LogCritical<T>(IEnumerable<T> iterable) => Log(LogLevel.Critical, iterable);
        /// <summary>
        /// Logs each key-value pair in a dictionary at the <see cref="LogLevel.Critical"/> level.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
        /// <param name="dict">The dictionary to log.</param>
        public void LogCritical<TKey, TValue>(IDictionary<TKey, TValue> dict) => Log(LogLevel.Critical, dict);

        /// <summary>
        /// Logs a message at the <see cref="LogLevel.Debug"/> level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogDebug(string message) => Log(LogLevel.Debug, message);
        /// <summary>
        /// Logs each item in an enumerable collection at the <see cref="LogLevel.Debug"/> level.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="iterable">The collection to log.</param>
        public void LogDebug<T>(IEnumerable<T> iterable) => Log(LogLevel.Debug, iterable);
        /// <summary>
        /// Logs each key-value pair in a dictionary at the <see cref="LogLevel.Debug"/> level.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
        /// <param name="dict">The dictionary to log.</param>
        public void LogDebug<TKey, TValue>(IDictionary<TKey, TValue> dict) => Log(LogLevel.Debug, dict);

        /// <summary>
        /// Logs a message at the <see cref="LogLevel.Error"/> level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogError(string message) => Log(LogLevel.Error, message);
        /// <summary>
        /// Logs each item in an enumerable collection at the <see cref="LogLevel.Error"/> level.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="iterable">The collection to log.</param>
        public void LogError<T>(IEnumerable<T> iterable) => Log(LogLevel.Error, iterable);
        /// <summary>
        /// Logs each key-value pair in a dictionary at the <see cref="LogLevel.Error"/> level.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
        /// <param name="dict">The dictionary to log.</param>
        public void LogError<TKey, TValue>(IDictionary<TKey, TValue> dict) => Log(LogLevel.Error, dict);

        /// <summary>
        /// Logs a message at the <see cref="LogLevel.Information"/> level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogInformation(string message) => Log(LogLevel.Information, message);
        /// <summary>
        /// Logs each item in an enumerable collection at the <see cref="LogLevel.Information"/> level.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="iterable">The collection to log.</param>
        public void LogInformation<T>(IEnumerable<T> iterable) => Log(LogLevel.Information, iterable);
        /// <summary>
        /// Logs each key-value pair in a dictionary at the <see cref="LogLevel.Information"/> level.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
        /// <param name="dict">The dictionary to log.</param>
        public void LogInformation<TKey, TValue>(IDictionary<TKey, TValue> dict) => Log(LogLevel.Information, dict);

        /// <summary>
        /// Logs a message at the <see cref="LogLevel.Trace"/> level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogTrace(string message) => Log(LogLevel.Trace, message);
        /// <summary>
        /// Logs each item in an enumerable collection at the <see cref="LogLevel.Trace"/> level.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="iterable">The collection to log.</param>
        public void LogTrace<T>(IEnumerable<T> iterable) => Log(LogLevel.Trace, iterable);
        /// <summary>
        /// Logs each key-value pair in a dictionary at the <see cref="LogLevel.Trace"/> level.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
        /// <param name="dict">The dictionary to log.</param>
        public void LogTrace<TKey, TValue>(IDictionary<TKey, TValue> dict) => Log(LogLevel.Trace, dict);

        /// <summary>
        /// Logs a message at the <see cref="LogLevel.Warning"/> level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogWarning(string message) => Log(LogLevel.Warning, message);
        /// <summary>
        /// Logs each item in an enumerable collection at the <see cref="LogLevel.Warning"/> level.
        /// </summary>
        /// <typeparam name="T">The type of items in the collection.</typeparam>
        /// <param name="iterable">The collection to log.</param>
        public void LogWarning<T>(IEnumerable<T> iterable) => Log(LogLevel.Warning, iterable);
        /// <summary>
        /// Logs each key-value pair in a dictionary at the <see cref="LogLevel.Warning"/> level.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
        /// <param name="dict">The dictionary to log.</param>
        public void LogWarning<TKey, TValue>(IDictionary<TKey, TValue> dict) => Log(LogLevel.Warning, dict);

        /// <summary>
        /// Logs an exception with an optional header message and log level.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="headerMessage">An optional header message to prefix the exception details.</param>
        /// <param name="logLevel">The log level to use for the exception. Defaults to <see cref="LogLevel.Error"/>.</param>
        public void LogException(Exception exception, string? headerMessage, LogLevel logLevel = LogLevel.Error)
        {
            if (exception is null) return;

            headerMessage ??= "Exception occurred:";
            string message = headerMessage;
            message += $"{Environment.NewLine}{exception}";

            var args = new LogEventArgs(logLevel, LogHandle, message, LogColor)
            {
                LogDateTime = LogUTCTime ? DateTime.UtcNow : DateTime.Now
            };

            // Write to sink if we have one (DI mode), but skip if Logger is a BaseLogger
            // to avoid dual sink writes (the inner BaseLogger will write to sink itself)
            if (Logger is not BaseLogger)
                _sink?.Write(args);

            // Pass through to Logger if we have one
            if (Logger != null)
            {
                if (!LogExceptionActions.TryGetValue(logLevel, out var logAction))
                    logAction = LogErrorException;
                logAction(Logger, headerMessage, exception);
            }

            OnLogEvent(args);
        }

        /// <summary>
        /// Logs an exception at the <see cref="LogLevel.Error"/> level with no header message.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        public void LogException(Exception exception) => LogException(exception, null, LogLevel.Error);

        /// <summary>
        /// Subscribes a synchronous handler to the <see cref="LogEvent"/> event.
        /// </summary>
        /// <param name="handler">The handler to invoke when a log event is raised.</param>
        /// <returns>
        /// A <see cref="LogEvent"/> delegate that can be used to unsubscribe, or <c>null</c> if the handler is <c>null</c>.
        /// </returns>
        public LogEvent? SubscribeLogEventSync(Action<object, LogEventArgs> handler)
        {
            if (handler is null) return null;

            Task wrapper(object sender, LogEventArgs e)
            {
                handler(sender, e);
                return Task.CompletedTask;
            }

            LogEvent += wrapper;
            return wrapper;
        }

        /// <summary>
        /// Raises the specified log event asynchronously, invoking all registered handlers.
        /// </summary>
        /// <param name="logEvent">The log event delegate to invoke.</param>
        /// <param name="eventArgs">The event arguments to pass to handlers.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1229:Use async/await when necessary", Justification = "Awaiting all tasks after select statement, need to trigger all invokers without delay")]
        private async Task OnRaiseLogEventAsync(LogEvent? logEvent, LogEventArgs eventArgs)
        {
            ArgumentNullException.ThrowIfNull(eventArgs, paramName: nameof(eventArgs));

            var localEvent = logEvent;
            if (localEvent is null) return;

            try
            {
                var handlers = localEvent.GetInvocationList();
                if (handlers is null) return;

                var tasks = handlers.Cast<LogEvent>()
                                    .Select(handler =>
                {
                    try
                    {
                        return handler.Invoke(this, eventArgs);
                    }
                    catch (Exception ex)
                    {
                        return Task.FromException(ex);
                    }
                });

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                if (Logger != null) LogErrorException(Logger, "Error when trying to raise log event, one or more subscribers failed", ex);
            }
        }

        /// <summary>
        /// Raises log events for the specified event arguments.
        /// Raises <see cref="LogEvent"/> for instance-level subscribers.
        /// </summary>
        /// <param name="eventArgs">The event arguments to pass to subscribers.</param>
        protected void OnLogEvent(LogEventArgs eventArgs)
        {
            _ = OnRaiseLogEventAsync(LogEvent, eventArgs);
        }

        /// <summary>
        /// Logs a formatted message with the specified log level, event ID, and state.
        /// </summary>
        /// <remarks>The method checks if logging is enabled for the specified <paramref name="logLevel"/>
        /// before proceeding. If logging is enabled, it formats the message using the provided <paramref
        /// name="formatter"/> and logs it.</remarks>
        /// <typeparam name="TState">The type of the state object to be logged.</typeparam>
        /// <param name="logLevel">The severity level of the log message.</param>
        /// <param name="eventId">The identifier for the event associated with the log message.</param>
        /// <param name="state">The state object containing information to be logged.</param>
        /// <param name="exception">The exception related to the log entry, if any. Can be <see langword="null"/>.</param>
        /// <param name="formatter">A function that formats the state and exception into a log message string.</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            string message = formatter(state, null);
            if (exception is null)
                Log(logLevel, message);
            else
                LogException(exception, message, logLevel);
        }

        /// <summary>
        /// Determines whether logging is enabled for the specified log level.
        /// </summary>
        /// <param name="logLevel">The log level to check against the current logging configuration.</param>
        /// <returns><see langword="true"/> if logging is enabled for the specified <paramref name="logLevel"/>; otherwise, <see
        /// langword="false"/>.</returns>
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel;

        /// <summary>
        /// Begins a logical operation scope.
        /// </summary>
        /// <typeparam name="TState">The type of the state to associate with the scope. Must be a non-nullable type.</typeparam>
        /// <param name="state">The state to associate with the scope. This value cannot be null.</param>
        /// <returns>An <see cref="IDisposable"/> that ends the logical operation scope on disposal.</returns>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return Logger?.BeginScope(state);
        }
    }
}
