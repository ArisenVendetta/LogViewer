using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.Extensions.Logging;

namespace LogViewer
{
    public interface ILoggable
    {
        public string LogHandle { get; }
        public Color LogColor { get; set; }

        public ILogger Logger { get; }
        public LogLevel LogLevel { get; }

        public event LogEventHandler? LogEvent;

        void Log(LogLevel level, string message);
        void Log<T>(LogLevel level, IEnumerable<T> iterable);
        void Log<TKey, TValue>(LogLevel level, IDictionary<TKey, TValue> dict);

        void LogInfo(string message);
        void LogInfo<T>(IEnumerable<T> iterable);
        void LogInfo<TKey, TValue>(IDictionary<TKey, TValue> dict);

        void LogDebug(string message);
        void LogDebug<T>(IEnumerable<T> iterable);
        void LogDebug<TKey, TValue>(IDictionary<TKey, TValue> dict);

        void LogCritical(string message);
        void LogCritical<T>(IEnumerable<T> iterable);
        void LogCritical<TKey, TValue>(IDictionary<TKey, TValue> dict);

        void LogWarning(string message);
        void LogWarning<T>(IEnumerable<T> iterable);
        void LogWarning<TKey, TValue>(IDictionary<TKey, TValue> dict);

        void LogError(string message);
        void LogError<T>(IEnumerable<T> iterable);
        void LogError<TKey, TValue>(IDictionary<TKey, TValue> dict);

        void LogTrace(string message);
        void LogTrace<T>(IEnumerable<T> iterable);
        void LogTrace<TKey, TValue>(IDictionary<TKey, TValue> dict);

        void LogException(Exception exception, string headerMessage, LogLevel logLevel = LogLevel.Error);
        void LogException(Exception exception);

        LogEventHandler? SubscribeLogEventSync(Action<object, LogEventArgs> handler);
    }
}
