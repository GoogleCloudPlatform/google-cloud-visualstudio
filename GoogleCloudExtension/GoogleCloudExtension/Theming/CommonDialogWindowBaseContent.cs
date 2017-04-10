// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using GoogleCloudExtension.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace GoogleCloudExtension.Theming
{
    /// <summary>
    /// This class implements all of the common visuals for all dialogs.
    /// </summary>
    public class CommonDialogWindowBaseContent : ContentControl
    {
        private const string DialogBannerPath = "Theming/Resources/GCP_logo_horizontal.png";

        private static readonly Lazy<ImageSource> s_dialogBanner = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(DialogBannerPath));

        // Dependency property registration for the buttons property, to allow template binding to work.
        public static readonly DependencyProperty ButtonsProperty =
            DependencyProperty.Register(
                nameof(Buttons),
                typeof(IList),
                typeof(CommonDialogWindowBaseContent));

        // Dependency property for the HasBanner property.
        public static readonly DependencyProperty HasBannerProperty =
            DependencyProperty.Register(
                nameof(HasBanner),
                typeof(bool),
                typeof(CommonDialogWindowBaseContent));

        // Dependency property for the ValidationResults property.
        public static readonly DependencyProperty ValidationResultsProperty =
            DependencyProperty.Register(
                nameof(ValidationResults),
                typeof(IList<ValidationResult>),
                typeof(CommonDialogWindowBaseContent),
                new FrameworkPropertyMetadata
                {
                    BindsTwoWayByDefault = true,
                    DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });

        /// <summary>
        /// The list of buttons to show in the dialog.
        /// </summary>
        public IList Buttons
        {
            get { return (IList)GetValue(ButtonsProperty); }
            set { SetValue(ButtonsProperty, value); }
        }

        /// <summary>
        /// Returns the banner to use for the dialog.
        /// </summary>
        public ImageSource Banner => s_dialogBanner.Value;

        /// <summary>
        /// Returns whether the banner is on or not.
        /// </summary>
        public bool HasBanner
        {
            get { return (bool)GetValue(HasBannerProperty); }
            set { SetValue(HasBannerProperty, value); }
        }

        public IList<ValidationResult> ValidationResults
        {
            get { return (IList<ValidationResult>)GetValue(ValidationResultsProperty); }
            set { SetValue(ValidationResultsProperty, value); }
        }

        public CommonDialogWindowBaseContent()
        {
            Initialize();
            Buttons = new BindableList<DialogButtonInfo>(this);
        }

        /// <summary>
        /// Due to a limitation on the Xaml parser with respect to names in the content of a ContentControl
        /// we need to load the visuals (the styles) programmatically. If we were to use the normal pattern we will
        /// get the "Can't apply Name to an element" error.
        /// </summary>
        private void Initialize()
        {
            Resources = new ResourceDictionary
            {
                Source = ResourceUtils.GetResourceUri("Theming/ThemingResources.xaml")
            };
        }
    }
}
