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

using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GoogleCloudExtension.AttachDebuggerDialog
{
    /// <summary>
    /// Interface that defines common methods/properties for attaching remote debugger steps.
    /// </summary>
    public interface IAttachDebuggerStep : INotifyPropertyChanged
    {
        /// <summary>
        /// Called for initialization or beginning the step.
        /// </summary>
        /// <returns>
        /// A reference to next step, indicates current step task is complete.
        /// null : Stay on current step, do not transite to next step.
        /// </returns>
        Task<IAttachDebuggerStep> OnStartAsync();

        /// <summary>
        /// Responds to OK button click event.
        /// </summary>
        /// <returns>
        /// A reference to next step.
        /// </returns>
        Task<IAttachDebuggerStep> OnOkCommandAsync();

        /// <summary>
        /// Responds to Cancel button click event.
        /// </summary>
        void OnCancelCommand();

        /// <summary>
        /// Returns the content of the publish step.
        /// </summary>
        ContentControl Content { get; }

        /// <summary>
        /// Returns if Cancel button should be enabled.
        /// </summary>
        bool IsCancelButtonEnabled { get; }

        /// <summary>
        /// Returns if OK button should be enabled.
        /// </summary>
        bool IsOkButtonEnabled { get; }

        /// <summary>
        /// Sets if cancel button should be visible.
        /// </summary>
        bool IsCancelButtonVisible { get; }
    }
}
