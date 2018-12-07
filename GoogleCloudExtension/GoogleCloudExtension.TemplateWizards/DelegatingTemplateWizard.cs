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

using EnvDTE;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace GoogleCloudExtension.TemplateWizards
{
    /// <summary>
    /// A template wizard that delegates to an implementation of IWizard received via MEF.
    /// </summary>
    /// <typeparam name="T">The type of the wizard to import and delegate to.</typeparam>
    public abstract class DelegatingTemplateWizard<T> : IWizard where T : class, IWizard
    {
        [Import]
        private T _wizard = default(T);

        ///<inheritdoc />
        public void RunStarted(
            object automationObject,
            Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind,
            object[] customParams)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var provider = (IServiceProvider)automationObject;
            var model = (IComponentModel)provider.QueryService<SComponentModel>();
            _wizard = model.GetService<T>();
            _wizard.RunStarted(automationObject, replacementsDictionary, runKind, customParams);
        }

        ///<inheritdoc />
        public void ProjectFinishedGenerating(Project project)
        {
            _wizard.ProjectFinishedGenerating(project);
        }

        ///<inheritdoc />
        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
            _wizard.ProjectItemFinishedGenerating(projectItem);
        }

        ///<inheritdoc />
        public bool ShouldAddProjectItem(string filePath)
        {
            return _wizard.ShouldAddProjectItem(filePath);
        }

        ///<inheritdoc />
        public void BeforeOpeningFile(ProjectItem projectItem)
        {
            _wizard.BeforeOpeningFile(projectItem);
        }

        ///<inheritdoc />
        public void RunFinished()
        {
            _wizard.RunFinished();
        }
    }
}