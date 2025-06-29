using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LogViewer.Converters
{
    /// <summary>
    /// Provides a value converter that inverts a boolean value.
    /// </summary>
    /// <remarks>This converter is typically used in data binding scenarios where a boolean value needs to be
    /// inverted. For example, it can be used to bind a `true` value to a property that expects `false`, and vice
    /// versa.</remarks>
    public class InverseBooleanConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to its negated equivalent. If the input is not a boolean, the original value is
        /// returned.
        /// </summary>
        /// <param name="value">The value to be converted. If the value is a boolean, it will be negated.</param>
        /// <param name="targetType">The type to convert to. This parameter is not used in the conversion process.</param>
        /// <param name="parameter">An optional parameter for the conversion. This parameter is not used in the conversion process.</param>
        /// <param name="culture">The culture to use in the conversion. This parameter is not used in the conversion process.</param>
        /// <returns>The negated boolean value if <paramref name="value"/> is a boolean; otherwise, the original <paramref
        /// name="value"/>.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                return !booleanValue;
            }
            return value; // Return the original value if it's not a boolean
        }

        /// <summary>
        /// Converts a value back to its original form by inverting a boolean value.
        /// </summary>
        /// <param name="value">The value to be converted back. Expected to be a boolean.</param>
        /// <param name="targetType">The type to which the value is being converted. This parameter is not used in this implementation.</param>
        /// <param name="parameter">An optional parameter for the conversion. This parameter is not used in this implementation.</param>
        /// <param name="culture">The culture information for the conversion. This parameter is not used in this implementation.</param>
        /// <returns>The inverted boolean value if <paramref name="value"/> is a boolean; otherwise, returns the original
        /// <paramref name="value"/>.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                return !booleanValue;
            }
            return value; // Return the original value if it's not a boolean
        }
    }
}
