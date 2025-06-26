using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.Extensions.Logging;

namespace LogViewer
{
    /// <summary>
    /// Represents the data for a single log event, including log level, handle, message, color, timestamp, and a unique identifier.
    /// Used for passing log information to event subscribers and for display in log viewers.
    /// </summary>
    /// <param name="level">The severity level of the log event.</param>
    /// <param name="logHandle">The handle (name) of the logger that generated the event.</param>
    /// <param name="message">The log message text.</param>
    /// <param name="color">The color associated with the logger or log event.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logHandle"/> or <paramref name="message"/> is null.</exception>
    public class LogEventArgs(LogLevel level, string logHandle, string message, Color color) : EventArgs, IEquatable<LogEventArgs>
    {
        /// <summary>
        /// Gets the handle (name) of the logger that generated this event.
        /// </summary>
        public string LogHandle { get; } = logHandle ?? throw new ArgumentNullException(nameof(logHandle));

        /// <summary>
        /// Gets the color associated with this log event.
        /// </summary>
        public Color LogColor { get; } = color;

        /// <summary>
        /// Gets the log message text.
        /// </summary>
        public string LogText { get; } = message ?? throw new ArgumentNullException(nameof(message));

        /// <summary>
        /// Gets or sets the timestamp for when the log event occurred.
        /// </summary>
        public DateTime LogDateTime { get; init; }

        /// <summary>
        /// Gets the formatted timestamp string for display, using the global log date/time format.
        /// </summary>
        public string LogDateTimeFormatted => LogDateTime.ToString(BaseLogger.LogDateTimeFormat, CultureInfo.InvariantCulture);

        /// <summary>
        /// Gets the severity level of the log event.
        /// </summary>
        public LogLevel LogLevel { get; } = level;

        /// <summary>
        /// Gets the unique identifier for this log event instance.
        /// </summary>
        public Guid ID { get; } = Guid.NewGuid();

        /// <summary>
        /// Returns the main parts of the log message as a tuple: timestamp, handle, and message body.
        /// </summary>
        /// <returns>A tuple containing the formatted timestamp, log handle, and log text.</returns>
        public (string Timestamp, string LogHandle, string Body) GetLogMessageParts()
        {
            return ($"{LogDateTime.ToString(BaseLogger.LogDateTimeFormat, CultureInfo.InvariantCulture)}", LogHandle, LogText);
        }

        /// <summary>
        /// Returns a string representation of the log event, suitable for display or logging.
        /// </summary>
        /// <returns>A formatted string containing the timestamp, handle, and message.</returns>
        public override string ToString()
        {
            return $"{LogDateTime.ToString(BaseLogger.LogDateTimeFormat, CultureInfo.InvariantCulture)} [{LogHandle}] {LogText}";
        }

        /// <summary>
        /// Determines whether the specified <see cref="LogEventArgs"/> is equal to the current instance, based on the unique ID.
        /// </summary>
        /// <param name="other">The other <see cref="LogEventArgs"/> to compare.</param>
        /// <returns>True if the IDs are equal; otherwise, false.</returns>
        public bool Equals(LogEventArgs? other)
        {
            if (other is null) return false;
            return ID == other.ID;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current instance, based on the unique ID.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True if the object is a <see cref="LogEventArgs"/> with the same ID; otherwise, false.</returns>
        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            return Equals(obj as LogEventArgs);
        }

        /// <summary>
        /// Returns a hash code for this instance, based on the unique ID.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode() => ID.GetHashCode();
    }
}
