using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LogViewer.Converters
{
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is System.Windows.Media.Color color)
            {
                return new System.Windows.Media.SolidColorBrush(color);
            }
            return System.Windows.Media.Brushes.Black; // Default brush if not a Color
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing; // No conversion back needed
        }
    }
}
