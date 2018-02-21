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
using System.Linq;

namespace GoogleCloudExtension.TemplateWizards.Dialogs.TemplateChooserDialog
{
    /// <summary>
    /// View model for the ASP.NET Core template chooser dialog.
    /// </summary>
    public class AspNetCoreTemplateChooserViewModel : TemplateChooserViewModelBase
    {
        private FrameworkType _selectedFramework;
        private AspNetVersion _selectedVersion;
        private IList<AspNetVersion> _availableVersions;
        private IList<FrameworkType> _availableFrameworks;

        private static readonly FrameworkType[] s_netCoreUnavailableFrameworks = { FrameworkType.NetFramework };

        private static readonly FrameworkType[] s_netCoreAvailabelFrameworks =
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
                    SelectedFramework = AvailableFrameworks.First();
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

        /// <param name="closeWindow">The action that will close the dialog.</param>
        public AspNetCoreTemplateChooserViewModel(Action closeWindow) : base(closeWindow)
        {
            bool netCoreAvailable = AspNetVersion.GetAvailableAspNetCoreVersions(FrameworkType.NetCore).Any();
            AvailableFrameworks = netCoreAvailable ? s_netCoreAvailabelFrameworks : s_netCoreUnavailableFrameworks;
        }

        protected override TemplateChooserViewModelResult CreateResult()
        {
            return new TemplateChooserViewModelResult(this);
        }
    }
}
