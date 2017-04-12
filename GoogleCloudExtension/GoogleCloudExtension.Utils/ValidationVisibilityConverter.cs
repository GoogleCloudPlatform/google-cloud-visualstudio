using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace GoogleCloudExtension.Utils
{
    public class ValidationVisibilityConverter : MarkupExtension, IValueConverter
    {
        private static bool IsDisplayableResult(ValidationResult r) => !r.IsValid || r.ErrorContent != null;
        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var enumerable = value as IEnumerable<object>;
            if (enumerable == null)
            {
                return Visibility.Collapsed;
            }

            var list = enumerable.ToList();
            if (!list.Any())
            {
                return Visibility.Collapsed;
            }

            if (list.Any(o => !(o is ValidationResult) && !(o is ValidationError)))
            {
                return Visibility.Visible;
            }

            if (list.OfType<ValidationResult>().Any(IsDisplayableResult))
            {
                return Visibility.Visible;
            }

            var errorContents = list.OfType<ValidationError>().Select(e => e.ErrorContent).ToList();
            if (errorContents.Any(c => !(c is ValidationResult)))
            {
                return Visibility.Visible;
            }

            if (errorContents.OfType<ValidationResult>().Any(IsDisplayableResult))
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Not Implemented.
        /// </summary>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
