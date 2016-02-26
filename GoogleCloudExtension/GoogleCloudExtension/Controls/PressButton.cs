// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System.Windows;
using System.Windows.Controls;

namespace GoogleCloudExtension.Controls
{
    public class PressButton : Button
    {
        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
            nameof(IsChecked), typeof(bool), typeof(PressButton));

        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }
    }
}
