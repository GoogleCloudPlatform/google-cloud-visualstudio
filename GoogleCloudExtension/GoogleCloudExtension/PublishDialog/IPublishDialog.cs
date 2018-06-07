﻿// Copyright 2016 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.Projects;
using GoogleCloudExtension.PublishDialog.Steps;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace GoogleCloudExtension.PublishDialog
{
    /// <summary>
    /// This interface is to be implemented by the Publish dialog and contains all of the services
    /// offered from the publish dialog to the steps that it hosts.
    /// 
    /// The publishing dialog is logically defined as a navigation stack of steps, which allows the user to go back
    /// to the previous step.
    /// </summary>
    public interface IPublishDialog
    {
        /// <summary>
        /// Returns the the VS project selected by the user.
        /// </summary>
        IParsedDteProject Project { get; }

        /// <summary>
        /// This method pushes the given <seealso cref="IPublishDialogStep"/> to the navigation stack and sets the step
        /// as the current displayed step in the dialog.
        /// </summary>
        /// <param name="step">The step to navigate to.</param>
        void NavigateToStep(IStepContent<IPublishDialogStep> step);

        /// <summary>
        /// Called from a step that wants to finish the flow. In essence closes the dialog.
        /// </summary>
        void FinishFlow();

        /// <summary>
        /// Event raised when <see cref="FinishFlow"/> is called by one of the steps.
        /// Gives the rest of the steps associated with this publish dialog an opportunity
        /// to cleanup.
        /// </summary>
        event EventHandler FlowFinished;

        /// <summary>
        /// Makes the dialog look "busy" as long as the <paramref name="task"/> is running.
        /// </summary>
        /// <param name="task">The task to track.</param>
        void TrackTask(Task task);
    }
}
