// Copyright 2017 Google Inc. All Rights Reserved.
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

namespace GoogleCloudExtension.PubSubWindows
{
    /// <summary>
    /// The window for the new Pub/Sub topic dialog.
    /// </summary>
    public class NewTopicWindow : CommonDialogWindowBase
    {
        public NewTopicViewModel ViewModel { get; }

        public NewTopicWindow(string projectId) :
            base(string.Format(GoogleCloudExtension.Resources.NewTopicWindowHeader, projectId))
        {
            ViewModel = new NewTopicViewModel(projectId, this);
            Content = new NewTopicWindowContent(ViewModel);
        }

        public static string PromptUser(string projectId)
        {
            var dialog = new NewTopicWindow(projectId);
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }
    }
}
