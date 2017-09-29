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

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Theming;
using GoogleCloudExtension.Utils;
using System;
using StringResources = GoogleCloudExtension.Resources;

namespace GoogleCloudExtension.AttachDebuggerDialog
{
    /// <summary>
    /// This class implements the window that hosts attaching remote debugger steps.
    /// </summary>
    public class AttachDebuggerWindow : CommonDialogWindowBase
    {
        private AttachDebuggerWindowViewModel ViewModel { get; }

        private AttachDebuggerWindow(Instance gceInstance)
            : base(string.Format(StringResources.AttachDebuggerWindowCaptionFormat,
                                 gceInstance.GetPublicIpAddress(),
                                 gceInstance.Name))
        {
            ViewModel = new AttachDebuggerWindowViewModel(gceInstance, this);
            Content = new AttachDebuggerWindowContent { DataContext = ViewModel };
        }

        /// <summary>
        /// Starts the attaching remote debugger wizard.
        /// </summary>
        /// <param name="gceInstance">A GCE windows VM <seealso cref="Instance"/> object.</param>
        public static void PromptUser(Instance gceInstance)
        {
            if (String.IsNullOrWhiteSpace(gceInstance.GetPublicIpAddress()))
            {
                UserPromptUtils.OkPrompt(
                    message: StringResources.AttachDebuggerAddPublicIpAddressMessage,
                    title: StringResources.UiDefaultPromptTitle);
                return;
            }

            var dialog = new AttachDebuggerWindow(gceInstance);
            EventsReporterWrapper.ReportEvent(RemoteDebuggerWindowOpenEvent.Create());
            dialog.ShowModal();
        }
    }
}
