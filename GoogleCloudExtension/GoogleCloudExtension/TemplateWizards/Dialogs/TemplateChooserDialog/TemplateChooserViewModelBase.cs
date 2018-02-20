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

using Google.Apis.CloudResourceManager.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Utils;
using System;

namespace GoogleCloudExtension.TemplateWizards.Dialogs.TemplateChooserDialog
{
    public abstract class TemplateChooserViewModelBase : ViewModelBase
    {
        private string _gcpProjectId;
        private AppType _appType = AppType.Mvc;

        protected TemplateChooserViewModelBase(Action closeWindow, Func<Project> promptPickProject)
        {
            GcpProjectId = CredentialsStore.Default.CurrentProjectId ?? "";
            OkCommand = new ProtectedCommand(
                () =>
                {
                    Result = CreateResult();
                    closeWindow();
                });
            SelectProjectCommand =
                new ProtectedCommand(() => GcpProjectId = promptPickProject()?.ProjectId ?? GcpProjectId);
        }

        protected abstract TemplateChooserViewModelResult CreateResult();

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
        /// The command to select an existing project.
        /// </summary>
        public ProtectedCommand SelectProjectCommand { get; }

        /// <summary>
        /// The result of the dialog.
        /// </summary>
        public TemplateChooserViewModelResult Result { get; private set; }
    }
}