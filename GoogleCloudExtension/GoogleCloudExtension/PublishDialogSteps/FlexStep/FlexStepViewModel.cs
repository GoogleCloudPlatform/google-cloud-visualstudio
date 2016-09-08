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

using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.Utils;
using System;
using System.Diagnostics;
using System.Windows;

namespace GoogleCloudExtension.PublishDialogSteps.FlexStep
{
    public class FlexStepViewModel : PublishDialogStepBase
    {
        private readonly FlexStepContent _content;
        private IPublishDialog _publishDialog;
        private string _version;
        private bool _promote = true;
        private bool _openWebsite = true;

        public string Version
        {
            get { return _version; }
            set { SetValueAndRaise(ref _version, value); }
        }

        public bool Promote
        {
            get { return _promote; }
            set { SetValueAndRaise(ref _promote, value); }
        }

        public bool OpenWebsite
        {
            get { return _openWebsite; }
            set { SetValueAndRaise(ref _openWebsite, value); }
        }

        private FlexStepViewModel(FlexStepContent content)
        {
            _content = content;

            CanPublish = true;
        }

        #region IPublishDialogStep

        public override FrameworkElement Content => _content;

        public override void OnPushedToDialog(IPublishDialog dialog)
        {
            _publishDialog = dialog;
        }

        public override async void Publish()
        {
            var context = new Context
            {
                CredentialsPath = CredentialsStore.Default.CurrentAccountPath,
                ProjectId = CredentialsStore.Default.CurrentProjectId,
                AppName = GoogleCloudExtensionPackage.ApplicationName,
                AppVersion = GoogleCloudExtensionPackage.ApplicationVersion,
            };
            var options = new NetCoreDeployment.DeploymentOptions
            {
                Version = Version,
                Promote = Promote,
                Context = context
            };
            var project = _publishDialog.Project;

            GcpOutputWindow.Activate();
            GcpOutputWindow.Clear();
            GcpOutputWindow.OutputLine(String.Format(Resources.GcePublishStepStartMessage, project.Name));

            _publishDialog.FinishFlow();

            NetCorePublishResult result;
            using (var frozen = StatusbarHelper.Freeze())
            using (var animationShown = StatusbarHelper.ShowDeployAnimation())
            using (var progress = StatusbarHelper.ShowProgressBar(Resources.FlexPublishProgressMessage))
            using (var deployingOperation = GoogleCloudExtensionPackage.GetDeploymentOperation())
            {
                result = await NetCoreDeployment.PublishProjectAsync(
                    project.FullPath,
                    options,
                    progress,
                    GcpOutputWindow.OutputLine);
            }

            if (result != null)
            {
                GcpOutputWindow.OutputLine(String.Format(Resources.FlexPublishSuccessMessage, project.Name));
                StatusbarHelper.SetText(Resources.PublishSuccessStatusMessage);

                var url = result.GetDeploymentUrl();
                GcpOutputWindow.OutputLine(String.Format(Resources.PublishUrlMessage, url));
                if (OpenWebsite)
                {
                    Process.Start(url);
                }
            }
            else
            {
                GcpOutputWindow.OutputLine(String.Format(Resources.FlexPublishFailedMessage, project.Name));
                StatusbarHelper.SetText(Resources.PublishFailureStatusMessage);
            }


        }

        #endregion

        internal static FlexStepViewModel CreateStep()
        {
            var content = new FlexStepContent();
            var viewModel = new FlexStepViewModel(content);
            content.DataContext = viewModel;

            return viewModel;
        }
    }
}
