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
using System;

namespace GoogleCloudExtension.TemplateWizards.Dialogs.TemplateChooserDialog
{
    /// <summary>
    /// View model for the ASP.NET template chooser dialog.
    /// </summary>
    public class AspNetTemplateChooserViewModel : TemplateChooserViewModelBase
    {
        public AspNetTemplateChooserViewModel(Action closeWindow, Func<Project> promptPickProject) : base(
            closeWindow, promptPickProject)
        {
        }

        protected override TemplateChooserViewModelResult CreateResult()
        {
            return new TemplateChooserViewModelResult(this);
        }
    }
}