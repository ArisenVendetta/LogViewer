using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LogViewer.Converters
{
    /// <summary>
    /// Converts a <see cref="System.Windows.Media.Color"/> value to a <see cref="System.Windows.Media.SolidColorBrush"/> for WPF data binding scenarios.
    /// </summary>
    /// <remarks>
    /// This converter is used in XAML bindings to convert color values to brushes for UI elements.
    /// </remarks>
    public class ColorToBrushConverter : IValueConverter
    {
        /// <summary>
        /// Converts a <see cref="System.Windows.Media.Color"/> to a <see cref="System.Windows.Media.SolidColorBrush"/>.
        /// </summary>
        /// <param name="value">The value produced by the binding source. Expected to be a <see cref="System.Windows.Media.Color"/>.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">Optional parameter to use in the converter (not used).</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A <see cref="System.Windows.Media.SolidColorBrush"/> with the specified color,
        /// or <see cref="System.Windows.Media.Brushes.Black"/> if the value is not a <see cref="System.Windows.Media.Color"/>.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is System.Windows.Media.Color color)
            {
                return new System.Windows.Media.SolidColorBrush(color);
            }
            return System.Windows.Media.Brushes.Black; // Default brush if not a Color
        }

        /// <summary>
        /// Not implemented. Conversion from <see cref="System.Windows.Media.Brush"/> back to <see cref="System.Windows.Media.Color"/> is not supported.
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
