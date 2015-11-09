// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// General booblean converter, converting True or False to the desired values.
    /// Note: Only Convert is implemented, so this is not a bidirectional converter, do not use on TwoWay bindings.
    /// </summary>
    public class BooleanConverter : IValueConverter
    {
        public object IfTrue { get; set; }
        public object IfFalse { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                return (bool)value ? IfTrue : IfFalse;
            }
            Debug.WriteLine($"Value should be boolean: {value}");
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
