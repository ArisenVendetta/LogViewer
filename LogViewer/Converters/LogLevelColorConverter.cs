using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Microsoft.Extensions.Logging;

namespace LogViewer.Converters
{
    public class LogLevelColorConverter : IValueConverter
    {
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
                    LogLevel.Error       => System.Windows.Media.Colors.OrangeRed,
                    LogLevel.Critical    => System.Windows.Media.Colors.Red,
                    _                    => System.Windows.Media.Colors.Black
                };
            }
            return new System.Windows.Media.SolidColorBrush(color);
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing; // No conversion back needed
        }
    }
}
