// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.DnxSupport;
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
            // This converter should only be called with supported runtimes, if an unsuported runtime is
            // passed in then the None runtime info will be used, which is not probably what is desired.
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
