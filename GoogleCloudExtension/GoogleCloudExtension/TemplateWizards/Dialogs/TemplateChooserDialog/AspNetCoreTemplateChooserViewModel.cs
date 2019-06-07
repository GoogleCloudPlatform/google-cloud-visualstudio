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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.VsVersion;

namespace GoogleCloudExtension.TemplateWizards.Dialogs.TemplateChooserDialog
{
    /// <summary>
    /// View model for the ASP.NET Core template chooser dialog.
    /// </summary>
    public class AspNetCoreTemplateChooserViewModel : TemplateChooserViewModelBase
    {
        internal const string DotnetCoreDownloadArchiveUrl = "https://github.com/dotnet/core/blob/master/release-notes/download-archive.md#net-core-tools-for-visual-studio-2015";
        private FrameworkType _selectedFramework;
        private AspNetVersion _selectedVersion;
        private IList<AspNetVersion> _availableVersions;
        private IList<FrameworkType> _availableFrameworks;
        private bool _netCoreMissingError;

        // Mockable static functions for testing.
        internal static Func<string, Process> StartProcessOverride { private get; set; } = null;
        private static Func<string, Process> StartProcess => StartProcessOverride ?? Process.Start;


        private static readonly FrameworkType[] s_netCoreUnavailableFrameworks = { FrameworkType.NetFramework };

        private static readonly FrameworkType[] s_netCoreAvailableFrameworks =
        {
            FrameworkType.NetCore,
            FrameworkType.NetFramework
        };

        public IList<FrameworkType> AvailableFrameworks
        {
            get { return _availableFrameworks; }
            set
            {
                SetValueAndRaise(ref _availableFrameworks, value);
                if (!AvailableFrameworks.Contains(SelectedFramework))
                {
                    SelectedFramework = AvailableFrameworks.FirstOrDefault();
                }
            }
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
                AvailableVersions = AspNetVersion.GetAvailableAspNetCoreVersions(SelectedFramework);
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
                    SelectedVersion = AvailableVersions.LastOrDefault();
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
        /// True if no usable versions of dotnet core are found in VS 2015.
        /// </summary>
        public bool NetCoreMissingError
        {
            get { return _netCoreMissingError; }
            private set { SetValueAndRaise(ref _netCoreMissingError, value); }
        }

        public ProtectedCommand OpenVisualStudio2015DotNetCoreToolingDownloadLink { get; } =
            new ProtectedCommand(() => StartProcess(DotnetCoreDownloadArchiveUrl));

        /// <param name="closeWindow">The action that will close the dialog.</param>
        public AspNetCoreTemplateChooserViewModel(Action closeWindow) : base(closeWindow)
        {
            OpenVisualStudio2015DotNetCoreToolingDownloadLink.CanExecuteCommand = false;
            bool netCoreAvailable = AspNetVersion.GetAvailableAspNetCoreVersions(FrameworkType.NetCore).Any();
            if (netCoreAvailable)
            {
                AvailableFrameworks = s_netCoreAvailableFrameworks;
            }
            else if (GoogleCloudExtensionPackage.Instance.VsVersion == VsVersionUtils.VisualStudio2015Version)
            {
                AvailableFrameworks = new List<FrameworkType>();
                NetCoreMissingError = true;
                OkCommand.CanExecuteCommand = false;
                OpenVisualStudio2015DotNetCoreToolingDownloadLink.CanExecuteCommand = true;
            }
            else
            {
                AvailableFrameworks = s_netCoreUnavailableFrameworks;
            }
        }

        /// <summary>
        /// The type of frameowork for the VS project to target, .NET Framework or .NET Core.
        /// </summary>
        public override FrameworkType GetSelectedFramework() => SelectedFramework;

        /// <summary>
        /// The ASP.NET or ASP.NET Core version for the VS project to use.
        /// </summary>
        public override AspNetVersion GetSelectedVersion() => SelectedVersion;
    }
}
