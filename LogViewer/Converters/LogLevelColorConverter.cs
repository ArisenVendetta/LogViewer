using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Microsoft.Extensions.Logging;

namespace LogViewer.Converters
{
    /// <summary>
    /// Converts a <see cref="LogLevel"/> value to a corresponding <see cref="System.Windows.Media.SolidColorBrush"/> for WPF data binding.
    /// </summary>
    /// <remarks>
    /// This converter is used in XAML to visually distinguish log messages by their severity.
    /// </remarks>
    public class LogLevelColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts a <see cref="LogLevel"/> to a <see cref="System.Windows.Media.SolidColorBrush"/> with a color representing the log level.
        /// </summary>
        /// <param name="value">The value produced by the binding source. Expected to be a <see cref="LogLevel"/>.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">Optional parameter to use in the converter (not used).</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A <see cref="System.Windows.Media.SolidColorBrush"/> with a color mapped to the specified <see cref="LogLevel"/>.
        /// Returns black if the value is not a <see cref="LogLevel"/>.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            System.Windows.Media.Color color = System.Windows.Media.Colors.Black;
            if (value is LogLevel logLevel)
            {
                 color = logLevel switch
                {
                    LogLevel.Trace       => System.Windows.Media.Colors.Gray,
                    LogLevel.Debug       => System.Windows.Media.Colors.Blue,
                    LogLevel.Warning     => System.Windows.Media.Colors.Orange,
                    LogLevel.Error       => System.Windows.Media.Colors.Red,
                    LogLevel.Critical    => System.Windows.Media.Colors.DarkRed,
                    _                    => System.Windows.Media.Colors.Black
                };
            }
            return new System.Windows.Media.SolidColorBrush(color);
        }

        /// <summary>
        /// Not implemented. Conversion from <see cref="System.Windows.Media.Brush"/> back to <see cref="LogLevel"/> is not supported.
        /// </summary>
        /// <param name="value">The value produced by the binding target (not used).</param>
        /// <param name="targetType">The type to convert to (not used).</param>
        /// <param name="parameter">Optional parameter to use in the converter (not used).</param>
        /// <param name="culture">The culture to use in the converter (not used).</param>
        /// <returns>
        /// Always returns <see cref="Binding.DoNothing"/>, indicating that no conversion is performed.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing; // No conversion back needed
        }
    }
}
