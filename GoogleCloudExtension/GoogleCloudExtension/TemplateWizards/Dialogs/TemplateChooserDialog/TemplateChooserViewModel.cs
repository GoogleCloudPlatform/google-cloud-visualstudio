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

using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.VsVersion;
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
        private bool _isMvc;
        private bool _isWebApi;
        private IList<FrameworkType> _availableFrameworks;

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
                if (_selectedFramework != value)
                {
                    SetValueAndRaise(ref _selectedFramework, value);
                    AvailableVersions = GetAvailableVersions();
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
        /// The list of availeble frameworks.
        /// </summary>
        public IList<FrameworkType> AvailableFrameworks
        {
            get { return _availableFrameworks; }
            private set
            {
                SetValueAndRaise(ref _availableFrameworks, value);
                if (!AvailableFrameworks.Contains(SelectedFramework))
                {
                    SelectedFramework = AvailableFrameworks.First();
                }
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
        /// If the app will be an MVC app.
        /// </summary>
        public bool IsMvc
        {
            get { return _isMvc; }
            set
            {
                SetValueAndRaise(ref _isMvc, value);
                OkCommand.CanExecuteCommand = IsMvc || IsWebApi;
            }
        }

        /// <summary>
        /// If the app will be a WebAPI app.
        /// </summary>
        public bool IsWebApi
        {
            get { return _isWebApi; }
            set
            {
                SetValueAndRaise(ref _isWebApi, value);
                OkCommand.CanExecuteCommand = IsMvc || IsWebApi;
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

        /// <param name="closeWindow">The action that will close the dialog.</param>
        /// <param name="promptPickProject">The function that will prompt the user to pick an existing project.</param>
        public TemplateChooserViewModel(Action closeWindow, Func<string> promptPickProject)
        {
            AvailableFrameworks = FrameworkType.GetAvailableFrameworks();
            GcpProjectId = CredentialsStore.Default.CurrentProjectId ?? "";
            OkCommand = new ProtectedCommand(
                () =>
                {
                    Result = new TemplateChooserViewModelResult(this);
                    closeWindow();
                },
                false);
            SelectProjectCommand = new ProtectedCommand(() => GcpProjectId = promptPickProject() ?? GcpProjectId);
        }

        private List<AspNetVersion> GetAvailableVersions()
        {
            switch (GoogleCloudExtensionPackage.VsVersion)
            {
                case VsVersionUtils.VisualStudio2015Version:
                    if (SelectedFramework == FrameworkType.NetFramework)
                    {
                        return new List<AspNetVersion> { AspNetVersion.AspNet4 };
                    }
                    else if (SelectedFramework == FrameworkType.NetCoreApp)
                    {
                        return new List<AspNetVersion>(AspNetVersion.Vs2015AspNetCoreVersions);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unknown Famework type: {SelectedFramework}");
                    }
                case VsVersionUtils.VisualStudio2017Version:
                    var versions = new List<AspNetVersion>(AspNetVersion.Vs2017AspNetCoreVersions);
                    if (SelectedFramework == FrameworkType.NetFramework)
                    {
                        versions.Add(AspNetVersion.AspNet4);
                    }
                    return versions;
                default:
                    throw new InvalidOperationException(
                        $"Unknown Visual Studio Version: {GoogleCloudExtensionPackage.VsVersion}");
            }
        }
    }
}
