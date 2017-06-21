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
using GoogleCloudExtension.TemplateWizards.Dialogs.ProjectIdDialog;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.VsVersion;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;

namespace GoogleCloudExtension.TemplateWizards
{
    /// <summary>
    /// Wizard for a project template.
    /// </summary>
    [Export(typeof(IGoogleProjectTemplateWizard))]
    public class GoogleProjectTemplateWizard : IGoogleProjectTemplateWizard
    {
        // Mockable static methods for testing.
        internal Func<string> PromptPickProjectId = PickProjectIdWindow.PromptUser;
        internal Action<string, bool> DeleteDirectory = Directory.Delete;

        private const string GlobalJsonFileName = "global.json";

        ///<inheritdoc />
        public void RunStarted(
            object automationObject,
            Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind,
            object[] customParams)
        {
            string projectId = PromptPickProjectId();
            if (projectId == null)
            {
                DeleteDirectory(replacementsDictionary["$destinationdirectory$"], true);
                bool isExclusive;
                if (bool.TryParse(replacementsDictionary["$exclusiveproject$"], out isExclusive) && isExclusive)
                {
                    DeleteDirectory(replacementsDictionary["$solutiondirectory$"], true);
                }
                throw new WizardBackoutException();
            }
            replacementsDictionary.Add("$gcpprojectid$", projectId);
            var solutionDir = new Uri(
                replacementsDictionary["$solutiondirectory$"].EnsureEndSeparator().Replace('\\', '/'));
            var packageDir = new Uri(solutionDir, "packages/");
            var projectDir = new Uri(
                replacementsDictionary["$destinationdirectory$"].EnsureEndSeparator().Replace('\\', '/'));
            string packagesPath = projectDir.MakeRelativeUri(packageDir).ToString();
            replacementsDictionary.Add("$packagespath$", packagesPath.Replace('/', Path.DirectorySeparatorChar));
        }

        ///<inheritdoc />
        public bool ShouldAddProjectItem(string filePath)
        {
            if (GlobalJsonFileName == Path.GetFileName(filePath))
            {
                return GoogleCloudExtensionPackage.VsVersion == VsVersionUtils.VisualStudio2015Version;
            }
            else
            {
                return true;
            }
        }

        ///<inheritdoc />
        public void RunFinished()
        {
        }

        ///<inheritdoc />
        public void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        ///<inheritdoc />
        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
        }

        ///<inheritdoc />
        public void ProjectFinishedGenerating(Project project)
        {
        }
    }
}
