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

using Newtonsoft.Json;

namespace GoogleCloudExtension.TemplateWizards.Dialogs.TemplateChooserDialog
{
    /// <summary>
    /// The results from the Template Chooser dialog.
    /// This is an immutable subset of data of a <see cref="TemplateChooserViewModel"/>.
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

        /// <summary>
        /// The type of application to create.
        /// </summary>
        public AppType AppType { get; }

        /// <param name="templateChooserViewModel">The view model this result object will pull its data from.</param>
        public TemplateChooserViewModelResult(TemplateChooserViewModel templateChooserViewModel)
        {
            GcpProjectId = templateChooserViewModel.GcpProjectId;
            SelectedFramework = templateChooserViewModel.SelectedFramework;
            SelectedVersion = templateChooserViewModel.SelectedVersion;
            AppType = templateChooserViewModel.AppType;
        }

        /// <summary>Constructor used for testing and for building from Json data.</summary>
        [JsonConstructor]
        internal TemplateChooserViewModelResult(
            string gcpProjectId,
            FrameworkType selectedFramework,
            AspNetVersion selectedVersion,
            AppType appType)
        {
            GcpProjectId = gcpProjectId;
            SelectedFramework = selectedFramework;
            SelectedVersion = selectedVersion;
            AppType = appType;
        }
    }
}