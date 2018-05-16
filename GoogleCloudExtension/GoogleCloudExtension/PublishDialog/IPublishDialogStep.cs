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

using System;
using System.ComponentModel;
using System.Windows;

namespace GoogleCloudExtension.PublishDialog
{
    /// <summary>
    /// Interface that defines the services offered by a publish dialog step.
    /// </summary>
    public interface IPublishDialogStep : INotifyDataErrorInfo, INotifyPropertyChanged
    {
        /// <summary>
        /// Returns whether the step can perform the publish operation.
        /// </summary>
        bool CanPublish { get; }

        /// <summary>
        /// Event raised whenever <seealso cref="CanPublish"/> value changes.
        /// </summary>
        event EventHandler CanPublishChanged;

        /// <summary>
        /// Returns the content of the publish step.
        /// </summary>
        FrameworkElement Content { get; }

        /// <summary>
        /// Performs the publish step action, will only be called if <seealso cref="CanPublish"/> return true.
        /// </summary>
        void Publish();

        /// <summary>
        /// Called every time that this step is at the top of the navigation stack and therefore visible.
        /// </summary>
        /// <param name="dialog">The dialog that is hosting this step.</param>
        void OnVisible(IPublishDialog dialog);
    }
}
