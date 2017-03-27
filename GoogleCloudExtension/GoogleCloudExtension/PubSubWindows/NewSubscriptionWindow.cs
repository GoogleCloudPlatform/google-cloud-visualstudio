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

using Google.Apis.Pubsub.v1.Data;
using GoogleCloudExtension.CloudExplorerSources.PubSub;
using GoogleCloudExtension.Theming;

namespace GoogleCloudExtension.PubSubWindows
{
    /// <summary>
    /// The window for the new pub sub subscription dialog.
    /// </summary>
    public class NewSubscriptionWindow : CommonDialogWindowBase
    {
        public NewSubscriptionViewModel ViewModel { get; }

        public NewSubscriptionWindow(string topicFullName) :
            base(
                string.Format(
                    GoogleCloudExtension.Resources.NewSubscriptionWindowHeader,
                    PubsubSource.GetPathLeaf(topicFullName)))
        {
            Subscription model = new Subscription { Topic = topicFullName };

            ViewModel = new NewSubscriptionViewModel(model, this);
            Content = new NewSubscriptionWindowContent(ViewModel);
        }

        public static Subscription PromptUser(string topicFullName)
        {
            var dialog = new NewSubscriptionWindow(topicFullName);
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }
    }
}