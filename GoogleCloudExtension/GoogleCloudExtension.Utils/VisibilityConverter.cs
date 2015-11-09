// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Converts a boolean value into a Visibility enum value. True will mean Visible and False Collapsed.
    /// Note: Only Convert is implemented, so this is not a bidirectional converter, do not use on TwoWay bindings.
    /// </summary>
    public class VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                return (bool)value ? Visibility.Visible : Visibility.Collapsed;
            }
            Debug.WriteLine($"Value should be a boolean: {value}");
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
