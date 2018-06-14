using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Checks if the string to convert is null or empty, and returns one of two values.
    /// </summary>
    /// <typeparam name="T">The types of the values to output.</typeparam>
    public abstract class NullEmptyConverter<T> : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// If true, null, empty and whitespace values will return <see cref="NotEmptyValue"/>
        /// instead of <see cref="EmptyValue"/>.
        /// </summary>
        public bool Invert { get; set; }

        protected abstract T NotEmptyValue { get; }

        protected abstract T EmptyValue { get; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string stringValue;
            if (value is IConvertible convertible)
            {
                stringValue = convertible.ToString(culture);
            }
            else
            {
                stringValue = value?.ToString();
            }

            return string.IsNullOrWhiteSpace(stringValue) ^ Invert ? EmptyValue : NotEmptyValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();

        /// <summary>
        /// Implement interface MarkupExtension.
        /// </summary>
        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}