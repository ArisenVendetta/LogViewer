using Newtonsoft.Json.Linq;
using CsvHelper.Configuration;

namespace LogViewer
{
    /// <summary>
    /// Maps the properties of the <see cref="LogEventArgs"/> class to CSV columns for serialization.
    /// </summary>
    /// <remarks>This class defines the mapping between the properties of <see cref="LogEventArgs"/> and their
    /// corresponding CSV column names and formats. It is used to configure how instances of <see cref="LogEventArgs"/>
    /// are serialized into CSV format, including custom formatting and value transformations.</remarks>
    public class LogEventArgsMap : ClassMap<LogEventArgs>
    {
        /// <summary>
        /// Configures the mapping of <see cref="LogEventArgs"/> properties to their corresponding CSV columns.
        /// </summary>
        /// <remarks>This constructor defines how each property of the <see cref="LogEventArgs"/> class is
        /// mapped to a CSV column, including custom formatting, naming, and value transformations where
        /// necessary.</remarks>
        public LogEventArgsMap()
        {
            Map(m => m.LogDateTime)
                .Name("Timestamp")
                .TypeConverterOption.Format(BaseLogger.LogDateTimeFormat);
            Map(m => m.LogLevel).Name("LogLevel");
            Map(m => m.ThreadId).Name("ThreadId");
            Map(m => m.LogColor)
                .Name("Color")
                .Convert(args => args.Value.LogColor.ToString());
            Map(m => m.LogHandle).Name("Handle");
            Map(m => m.LogText)
                .Name("Message")
                .Convert(args =>
                {
                    string sanitized = args.Value.LogText.Replace(Environment.NewLine, "{newline}");
                    sanitized = sanitized.Replace("\"", "\"\""); // Escape double quotes for CSV
                    return $"\"{sanitized}\"";
                });
        }
    }
}