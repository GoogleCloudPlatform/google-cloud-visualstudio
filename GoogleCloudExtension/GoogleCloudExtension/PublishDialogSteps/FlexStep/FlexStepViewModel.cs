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
        private bool _promote;
        private bool _openWebsite;

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

            var result = await NetCoreDeployment.PublishProjectAsync(
                project.FullPath,
                options,
                (l) => GcpOutputWindow.OutputLine(l));
            if (result!= null)
            {
                GcpOutputWindow.OutputLine($"Project {project.Name} deployed to App Engine Flex.");
                if (OpenWebsite)
                {
                    var url = result.GetDeploymentUrl();
                    GcpOutputWindow.OutputLine($"Opening webiste {url}");
                    Process.Start(url);
                }
            }
            else
            {
                GcpOutputWindow.OutputLine($"Failed to deploy project {project.Name} to App Engine Flex.");
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
