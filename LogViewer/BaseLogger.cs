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
    /// Base class for providing logging functionality, routing all log messages to a central real-time log viewer
    /// </summary>
    public abstract partial class BaseLogger : ILoggable
    {
        protected BaseLogger(string? handle = null, Color? color = null, LogLevel logLevel = LogLevel.Information)
        {
            if (LoggerFactory is null) throw new InvalidOperationException($"Must call {nameof(BaseLogger)}.{nameof(Initialize)} before creating an instance of {nameof(BaseLogger)}");
            if (ExcludeCharsFromName is null) ExcludeCharsFromName = [];

            string sanitizedHandle = handle ?? GetType().Name;
            foreach (var c in ExcludeCharsFromName.Union([' '])) sanitizedHandle = sanitizedHandle.Replace(c.ToString(), "").Trim();
            LogHandle = sanitizedHandle;
            LogColor = color ?? Colors.Black;
            LogLevel = logLevel;

            Logger = LoggerFactory.CreateLogger(LogHandle);
        }

        public string LogHandle { get; }
        public Color LogColor { get; set; }
        public ILogger Logger { get; }
        public LogLevel LogLevel { get; } = LogLevel.Information;

        public event LogEventHandler? LogEvent;

        public void Log(LogLevel level, string message)
        {
            if (message is null) return;

            var args = new LogEventArgs(level, LogHandle, message, LogColor)
            {
                LogDateTime = LogUTCTime ? DateTime.Now.ToUniversalTime() : DateTime.Now.ToLocalTime()
            };

            Logger.Log(level, args.LogText);

            OnLogEvent(args); // this should never raise an exception, everything is caught/logged within that method if there is an error
        }

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
                Logger?.LogError(ex, $"Error when attempting to log iterable of type: {typeof(T).Name}");
            }
        }

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
                Logger?.LogError(ex, $"Error when attempting to log dictionary of types - Key: {typeof(TKey).Name}, Value: {typeof(TValue).Name}");
            }
        }

        public void LogCritical(string message) => Log(LogLevel.Critical, message);
        public void LogCritical<T>(IEnumerable<T> iterable) => Log(LogLevel.Critical, iterable);
        public void LogCritical<TKey, TValue>(IDictionary<TKey, TValue> dict) => Log(LogLevel.Critical, dict);

        public void LogDebug(string message) => Log(LogLevel.Debug, message);
        public void LogDebug<T>(IEnumerable<T> iterable) => Log(LogLevel.Debug, iterable);
        public void LogDebug<TKey, TValue>(IDictionary<TKey, TValue> dict) => Log(LogLevel.Debug, dict);

        public void LogError(string message) => Log(LogLevel.Error, message);
        public void LogError<T>(IEnumerable<T> iterable) => Log(LogLevel.Error, iterable);
        public void LogError<TKey, TValue>(IDictionary<TKey, TValue> dict) => Log(LogLevel.Error, dict);


        public void LogInfo(string message) => Log(LogLevel.Information, message);
        public void LogInfo<T>(IEnumerable<T> iterable) => Log(LogLevel.Information, iterable);
        public void LogInfo<TKey, TValue>(IDictionary<TKey, TValue> dict) => Log(LogLevel.Information, dict);

        public void LogTrace(string message) => Log(LogLevel.Trace, message);
        public void LogTrace<T>(IEnumerable<T> iterable) => Log(LogLevel.Trace, iterable);
        public void LogTrace<TKey, TValue>(IDictionary<TKey, TValue> dict) => Log(LogLevel.Trace, dict);

        public void LogWarning(string message) => Log(LogLevel.Warning, message);
        public void LogWarning<T>(IEnumerable<T> iterable) => Log(LogLevel.Warning, iterable);
        public void LogWarning<TKey, TValue>(IDictionary<TKey, TValue> dict) => Log(LogLevel.Warning, dict);

        public void LogException(Exception exception, string? headerMessage, LogLevel logLevel = LogLevel.Error)
        {
            if (exception is null) return;

            string message = string.IsNullOrWhiteSpace(headerMessage) ? "Exception occured:" : headerMessage;
            message += $"{Environment.NewLine}{exception}";

            var args = new LogEventArgs(logLevel, LogHandle, message, LogColor)
            {
                LogDateTime = LogUTCTime ? DateTime.Now.ToUniversalTime() : DateTime.Now.ToLocalTime()
            };

            Logger.Log(logLevel: logLevel, exception: exception, message: args.LogText);

            OnLogEvent(args); // this should never raise an exception, everything is caught/logged within that method if there is an error
        }
        public void LogException(Exception exception) => LogException(exception, null, LogLevel.Error);


        public LogEventHandler? SubscribeLogEventSync(Action<object, LogEventArgs> handler)
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1229:Use async/await when necessary", Justification = "Awaiting all tasks after select statement, need to trigger all invokers without delay")]
        private async Task OnRaiseLogEventAsync(LogEventHandler? logEvent, LogEventArgs eventArgs)
        {
            if (eventArgs is null) throw new ArgumentNullException(nameof(eventArgs));
            var localEvent = logEvent;
            if (localEvent is null) return;

            try
            {
                var handlers = localEvent.GetInvocationList();
                if (handlers is null) return;

                var tasks = handlers.Cast<LogEventHandler>()
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
                Logger?.LogError(ex, "Error when trying to raise log event, one or more subscribers failed");
            }
        }

        protected void OnLogEvent(LogEventArgs eventArgs)
        {
            _ = OnRaiseLogEventAsync(DebugLogEvent, eventArgs);
            _ = OnRaiseLogEventAsync(LogEvent, eventArgs);
        }
    }
}
