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

        public static bool PromptUser(string projectId, out string topicName)
        {
            var dialog = new CommonDialogWindowBase(GoogleCloudExtension.Resources.NewTopicWindowTitle)
            {
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                HasMinimizeButton = false,
                HasMaximizeButton = false
            };
            NewTopicViewModel newTopicViewModel = new NewTopicViewModel(projectId, new WeakCommand(() =>
            {
                dialog.DialogResult = true;
                dialog.Close();
            }));
            dialog.Content = new NewTopicWindowContent(newTopicViewModel);
            var returnVal = dialog.ShowModal() == true;
            topicName = newTopicViewModel.TopicName;
            return returnVal;
        }
    }
}
