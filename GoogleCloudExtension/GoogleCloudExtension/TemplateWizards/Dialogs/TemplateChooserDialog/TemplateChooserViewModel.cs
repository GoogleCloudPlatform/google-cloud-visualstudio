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

using Google.Apis.CloudResourceManager.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleCloudExtension.TemplateWizards.Dialogs.TemplateChooserDialog
{
    /// <summary>
    /// View Model for the Template Chooser dialog.
    /// </summary>
    public class TemplateChooserViewModel : ViewModelBase
    {
        private string _gcpProjectId;
        private FrameworkType _selectedFramework;
        private AspNetVersion _selectedVersion;
        private IList<AspNetVersion> _availableVersions;
        private AppType _appType = AppType.Mvc;

        /// <summary>
        /// The id of a google cloud project.
        /// </summary>
        public string GcpProjectId
        {
            get { return _gcpProjectId; }
            set { SetValueAndRaise(ref _gcpProjectId, value); }
        }

        /// <summary>
        /// The selected framework.
        /// </summary>
        public FrameworkType SelectedFramework
        {
            get { return _selectedFramework; }
            set
            {
                SetValueAndRaise(ref _selectedFramework, value);
                AvailableVersions = AspNetVersion.GetAvailableVersions(SelectedFramework);
            }
        }

        /// <summary>
        /// The list of available versions.
        /// </summary>
        public IList<AspNetVersion> AvailableVersions
        {
            get { return _availableVersions; }
            private set
            {
                SetValueAndRaise(ref _availableVersions, value);
                if (!AvailableVersions.Contains(SelectedVersion))
                {
                    SelectedVersion = AvailableVersions.First();
                }
            }
        }

        /// <summary>
        /// The selected version.
        /// </summary>
        public AspNetVersion SelectedVersion
        {
            get { return _selectedVersion; }
            set { SetValueAndRaise(ref _selectedVersion, value); }
        }

        /// <summary>
        /// True if the <see cref="AppType"/> is <see cref="TemplateChooserDialog.AppType.Mvc"/>
        /// </summary>
        public bool IsMvc
        {
            get { return AppType == AppType.Mvc; }
            set
            {
                if (value)
                {
                    AppType = AppType.Mvc;
                }
                else if (AppType == AppType.Mvc)
                {
                    AppType = AppType.None;
                }
            }
        }

        /// <summary>
        /// True if the <see cref="AppType"/> is <see cref="TemplateChooserDialog.AppType.WebApi"/>
        /// </summary>
        public bool IsWebApi
        {
            get { return AppType == AppType.WebApi; }
            set
            {
                if (value)
                {
                    AppType = AppType.WebApi;
                }
                else if (AppType == AppType.WebApi)
                {
                    AppType = AppType.None;
                }
            }
        }

        /// <summary>
        /// The type of the app, MVC or WebAPI.
        /// </summary>
        public AppType AppType
        {
            get { return _appType; }
            private set
            {
                SetValueAndRaise(ref _appType, value);
                OkCommand.CanExecuteCommand = AppType != AppType.None;
            }
        }

        /// <summary>
        /// The command to complete the dialog.
        /// </summary>
        public ProtectedCommand OkCommand { get; }

        /// <summary>
        /// The command to select an existing project.
        /// </summary>
        public ProtectedCommand SelectProjectCommand { get; }

        /// <summary>
        /// The result of the dialog.
        /// </summary>
        public TemplateChooserViewModelResult Result { get; private set; }

        public bool NetCoreAvailable { get; } = IsNetCoreAvailable();

        private static bool IsNetCoreAvailable() => AspNetVersion.GetAvailableVersions(FrameworkType.NetCore).Any();

        /// <param name="closeWindow">The action that will close the dialog.</param>
        /// <param name="promptPickProject">The function that will prompt the user to pick an existing project.</param>
        public TemplateChooserViewModel(Action closeWindow, Func<Project> promptPickProject)
        {
            GcpProjectId = CredentialsStore.Default.CurrentProjectId ?? "";
            OkCommand = new ProtectedCommand(
                () =>
                {
                    Result = new TemplateChooserViewModelResult(this);
                    closeWindow();
                });
            SelectProjectCommand =
                new ProtectedCommand(() => GcpProjectId = promptPickProject()?.ProjectId ?? GcpProjectId);
            SelectedFramework = NetCoreAvailable ? FrameworkType.NetCore : FrameworkType.NetFramework;
        }
    }
}
