using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.Extensions.Logging;

namespace LogViewer
{
    public class LogEventArgs(LogLevel level, string logHandle, string message, Color color) : EventArgs, IEquatable<LogEventArgs>
    {
        public string LogHandle { get; } = logHandle ?? throw new ArgumentNullException(nameof(logHandle));
        public Color LogColor { get; } = color;
        public string LogText { get; } = message ?? throw new ArgumentNullException(nameof(message));
        public DateTime LogDateTime { get; init; }
        public string LogDateTimeFormatted => LogDateTime.ToString(BaseLogger.LogDateTimeFormat);
        public LogLevel LogLevel { get; } = level;
        public Guid Guid { get; } = Guid.NewGuid();

        public (string Timestamp, string LogHandle, string Body) GetLogMessageParts()
        {
            return ($"{LogDateTime.ToString(BaseLogger.LogDateTimeFormat)}", LogHandle, LogText);
        }

        public override string ToString()
        {
            return $"{LogDateTime.ToString(BaseLogger.LogDateTimeFormat)} [{LogHandle}] {LogText}";
        }

        public bool Equals(LogEventArgs? other)
        {
            if (other is null) return false;
            return Guid == other.Guid;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            return Equals(obj as LogEventArgs);
        }

        public override int GetHashCode() => Guid.GetHashCode();
    }
}
