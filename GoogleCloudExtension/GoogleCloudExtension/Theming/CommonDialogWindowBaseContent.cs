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
using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace GoogleCloudExtension.Theming
{
    /// <summary>
    /// This class implements all of the common visuals for all dialogs.
    /// </summary>
    public class CommonDialogWindowBaseContent : ContentControl
    {
        // Dependency property registration for the buttons property, to allow template binding to work.
        public static readonly DependencyProperty ButtonsProperty =
            DependencyProperty.Register(
                nameof(Buttons),
                typeof(IList),
                typeof(CommonDialogWindowBaseContent));

        /// <summary>
        /// The list of buttons to show in the dialog.
        /// </summary>
        public IList Buttons
        {
            get { return (IList)GetValue(ButtonsProperty); }
            set { SetValue(ButtonsProperty, value); }
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
            ResourceDictionary resources = new ResourceDictionary();
            resources.Source = ResourceUtils.GetResourceUri("Theming/ThemingResources.xaml");
            this.Resources = resources;
        }
    }
}
