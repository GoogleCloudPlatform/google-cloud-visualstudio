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

using Google.Apis.Pubsub.v1.Data;
using GoogleCloudExtension.Theming;
using GoogleCloudExtension.Utils;
using System.Windows;

namespace GoogleCloudExtension.PubSubWindows
{
    /// <summary>
    /// Interaction logic for NewSubscriptionWindow.xaml
    /// </summary>
    public partial class NewSubscriptionWindowContent
    {
        public NewSubscriptionWindowContent(NewSubscriptionViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        public static bool PromptUser(string topicFullName, out Subscription model)
        {
            var dialog = new CommonDialogWindowBase(GoogleCloudExtension.Resources.NewSubscriptionWindowTitle)
            {
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                HasMinimizeButton = false,
                HasMaximizeButton = false
            };

            model = new Subscription { Topic = topicFullName };
            var createCommand = new WeakCommand(() =>
            {
                dialog.DialogResult = true;
                dialog.Close();
            });

            NewSubscriptionViewModel viewModel = new NewSubscriptionViewModel(model, createCommand);
            dialog.Content = new NewSubscriptionWindowContent(viewModel);
            return dialog.ShowModal() == true;
        }
    }
}
