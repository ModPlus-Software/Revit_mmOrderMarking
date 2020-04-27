namespace mmOrderMarking.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    /// <summary>
    /// Enum to int two way converter
    /// </summary>
    public class EnumToIntConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && parameter is Type type)
            {
                return (int)Enum.Parse(type, value.ToString());
            }

            return value;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && parameter is Type type)
            {
                return (Enum)Enum.Parse(type, value.ToString());
            }

            return value;
        }
    }
}
