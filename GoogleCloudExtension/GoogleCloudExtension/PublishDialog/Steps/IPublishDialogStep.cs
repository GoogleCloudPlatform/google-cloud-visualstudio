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
using System.ComponentModel;

namespace GoogleCloudExtension.PublishDialog.Steps
{
    /// <summary>
    /// Interface that defines the services offered by a publish dialog step.
    /// </summary>
    public interface IPublishDialogStep : INotifyDataErrorInfo, INotifyPropertyChanged
    {
        /// <summary>
        /// The Command linked to the Next/Publish button.
        /// </summary>
        IProtectedCommand ActionCommand { get; }

        /// <summary>
        /// The title of the publish dialog in this step.
        /// </summary>
        string Title { get; }

        /// <summary>
        /// The Caption of the Next/Publish button.
        /// </summary>
        string ActionCaption { get; }

        /// <summary>
        /// Called every time this step moves on to the top of the navigation stack.
        /// </summary>
        /// <param name="previousStep">The previously visible step.</param>
        void OnVisible(IPublishDialogStep previousStep);

        /// <summary>
        /// Called every time this step moves off the top of the navigation stack.
        /// </summary>
        void OnNotVisible();
    }
}
