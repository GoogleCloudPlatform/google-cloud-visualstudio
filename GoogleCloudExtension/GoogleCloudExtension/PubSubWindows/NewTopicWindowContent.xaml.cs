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
using System.Windows;

namespace GoogleCloudExtension.PubSubWindows
{
    /// <summary>
    /// Interaction logic for NewTopicWindow.xaml
    /// </summary>
    public partial class NewTopicWindowContent
    {

        public NewTopicWindowContent(NewTopicViewModel newTopicViewModel)
        {
            InitializeComponent();
            DataContext = newTopicViewModel;
        }

        public static string PromptUser(string projectId)
        {
            var dialog = new CommonDialogWindowBase(GoogleCloudExtension.Resources.NewTopicWindowTitle, 600, 400)
            {
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                HasMinimizeButton = false,
                HasMaximizeButton = false
            };
            NewTopicViewModel newTopicViewModel = new NewTopicViewModel(projectId, dialog);
            dialog.Content = new NewTopicWindowContent(newTopicViewModel);
            if (dialog.ShowModal() == true)
            {
                return newTopicViewModel.TopicName;
            }
            else
            {
                return null;
            }
        }
    }
}
