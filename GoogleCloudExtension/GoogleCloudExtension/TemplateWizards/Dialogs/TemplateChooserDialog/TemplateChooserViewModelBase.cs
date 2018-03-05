// Copyright 2018 Google Inc. All Rights Reserved.
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
using System;

namespace GoogleCloudExtension.TemplateWizards.Dialogs.TemplateChooserDialog
{
    /// <summary>
    /// This class contains the common functionality for VS Template chooser dialogs.
    /// </summary>
    public abstract class TemplateChooserViewModelBase : ViewModelBase
    {
        private string _gcpProjectId;
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
        /// The result of the dialog.
        /// </summary>
        public TemplateChooserViewModelResult Result { get; private set; }

        /// <param name="closeWindow">The action that closes the related dialog.</param>
        protected TemplateChooserViewModelBase(Action closeWindow)
        {
            GcpProjectId = CredentialsStore.Default.CurrentProjectId ?? "";
            OkCommand = new ProtectedCommand(
                () =>
                {
                    Result = CreateResult();
                    closeWindow();
                });
        }

        /// <summary>
        /// Creates a <see cref="TemplateChooserViewModelResult"/> to store in <see cref="Result"/> Ok button is hit.
        /// </summary>
        private TemplateChooserViewModelResult CreateResult()
        {
            return new TemplateChooserViewModelResult(this);
        }

        /// <summary>
        /// The type of frameowork for the VS project to target, .NET Framework or .NET Core.
        /// </summary>
        public abstract FrameworkType GetSelectedFramework();

        /// <summary>
        /// The ASP.NET or ASP.NET Core version for the VS project to use.
        /// </summary>
        public abstract AspNetVersion GetSelectedVersion();
    }
}