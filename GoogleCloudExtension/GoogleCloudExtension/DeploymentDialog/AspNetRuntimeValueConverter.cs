// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud.Dnx;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GoogleCloudExtension.DeploymentDialog
{
    /// <summary>
    /// This class converts from a runtime enum to its display name.
    /// </summary>
    internal class AspNetRuntimeValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DnxRuntime)
            {
                var runtime = (DnxRuntime)value;
                return DnxRuntimeInfo.GetRuntimeInfo(runtime).DisplayName;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
