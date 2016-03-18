// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Use this converter to log binding information going through it.
    /// </summary>
    public class LoggingConverter : IValueConverter
    {
        public string Prefix { get; set; }

        public IValueConverter NestedConverter { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.WriteLine($"LoggingConverter.Convert: {Prefix}: {value}");
            if (NestedConverter != null)
            {
                value = NestedConverter.Convert(value, targetType, parameter, culture);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Debug.WriteLine($"LoggingConverter.ConvertBack: {Prefix}: {value}");
            if (NestedConverter != null)
            {
                value = NestedConverter.ConvertBack(value, targetType, parameter, culture);
            }
            return value;
        }
    }
}
