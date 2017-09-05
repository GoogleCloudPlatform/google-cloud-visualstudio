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

namespace GoogleCloudExtension.TemplateWizards.Dialogs.TemplateChooserDialog
{
    /// <summary>
    /// The results from the Template Chooser dialog.
    /// </summary>
    public class TemplateChooserViewModelResult
    {
        /// <summary>
        /// The Google Cloud Project ID.
        /// </summary>
        public string GcpProjectId { get; }

        /// <summary>
        /// The selected framework type.
        /// </summary>
        public FrameworkType SelectedFramework { get; }

        /// <summary>
        /// The selected ASP.NET version.
        /// </summary>
        public AspNetVersion SelectedVersion { get; }

        private readonly bool _isMvc;

        private readonly bool _isWebApi;

        /// <summary>
        /// The type of application to create.
        /// </summary>
        public string AppType
        {
            get
            {
                if (_isMvc)
                {
                    return "MVC";
                }
                else if (_isWebApi)
                {
                    return "WebAPI";
                }
                else
                {
                    throw new InvalidOperationException("Result should be either MVC or WebAPI.");
                }
            }
        }

        /// <param name="templateChooserViewModel">The view model this results object will pull its data from.</param>
        public TemplateChooserViewModelResult(TemplateChooserViewModel templateChooserViewModel)
        {
            GcpProjectId = templateChooserViewModel.GcpProjectId;
            SelectedFramework = templateChooserViewModel.SelectedFramework;
            SelectedVersion = templateChooserViewModel.SelectedVersion;
            _isMvc = templateChooserViewModel.IsMvc;
            _isWebApi = templateChooserViewModel.IsWebApi;
        }
    }
}