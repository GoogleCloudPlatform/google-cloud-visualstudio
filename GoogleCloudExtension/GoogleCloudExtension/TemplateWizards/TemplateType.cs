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

namespace GoogleCloudExtension.TemplateWizards
{
    /// <summary>
    /// Indicates the Template file that called the template wizard.
    /// </summary>
    public enum TemplateType
    {
        /// <summary>
        /// The GCP ASP.NET template.
        /// </summary>
        /// Created from (templatePath)\Gcp.AspNet.vstemplate
        AspNet,

        /// <summary>
        /// The GCP ASP.NET Core template.
        /// </summary>
        /// Created from (templatePath)\Gcp.AspNetCore.vstemplate
        AspNetCore
    }
}
