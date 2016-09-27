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

using GoogleCloudExtension.Theming;
using GoogleCloudExtension.Utils;
using System;
using System.Windows;

namespace GoogleCloudExtension.PubSubWindows
{
    /// <summary>
    /// Interaction logic for NewSubscriptionWindow.xaml
    /// </summary>
    public partial class NewSubscriptionWindowContent
    {
        private WeakCommand _onClick;

        public NewSubscriptionWindowContent(NewSubscriptionData data, Action onCreateClick)
        {
            InitializeComponent();
            DataContext = data;
            _onClick = new WeakCommand(onCreateClick);
        }

        private void createButton_OnClick(object sender, EventArgs args)
        {
            _onClick.Execute(null);
        }

        public static bool PromptUser(string fullName, out NewSubscriptionData data)
        {
            var dialog = new CommonDialogWindowBase(GoogleCloudExtension.Resources.PubSubNewSubscriptionWindowHeader)
            {
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.CanResize,
                HasMinimizeButton = false,
                HasMaximizeButton = false
            };
            data = new NewSubscriptionData(fullName);
            dialog.Content = new NewSubscriptionWindowContent(data, () =>
            {
                dialog.DialogResult = true;
                dialog.Close();
            });
            return dialog.ShowModal() == true;
        }
    }
}
