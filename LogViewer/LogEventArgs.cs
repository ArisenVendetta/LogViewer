using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.Extensions.Logging;

namespace LogViewer
{
    public class LogEventArgs(LogLevel level, string logHandle, string message, Color color) : EventArgs
    {
        public string LogHandle { get; } = logHandle ?? throw new ArgumentNullException(nameof(logHandle));
        public Color LogColor { get; } = color;
        public string LogText { get; } = message ?? throw new ArgumentNullException(nameof(message));
        public DateTime LogDateTime { get; init; }
        public LogLevel LogLevel { get; } = level;

        public (string Timestamp, string LogHandle, string Body) GetLogMessageParts()
        {
            return ($"{LogDateTime.ToString(BaseLogger.LogDateTimeFormat)}", LogHandle, LogText);
        }

        public override string ToString()
        {
            return $"{LogDateTime.ToString(BaseLogger.LogDateTimeFormat)} [{LogHandle}] {LogText}";
        }
    }
}
