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
using GoogleCloudExtension.PickProjectDialog;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using GcpProject = Google.Apis.CloudResourceManager.v1.Data.Project;
using VsProject = EnvDTE.Project;

namespace GoogleCloudExtension.TemplateWizards
{
    /// <summary>
    /// Wizard for a project template.
    /// </summary>
    [Export(typeof(IGoogleProjectTemplateWizard))]
    public class GoogleProjectTemplateWizard : IGoogleProjectTemplateWizard
    {
        // Mockable static methods for testing.
        internal Func<GcpProject> PromptPickProjectId =
            () => PickProjectIdWindow.PromptUser(Resources.TemplateWizardPickProjectIdHelpText, allowAccountChange: false);
        internal Action<Dictionary<string, string>> CleanupDirectories = GoogleTemplateWizardHelper.CleanupDirectories;

        ///<inheritdoc />
        public void RunStarted(
            object automationObject,
            Dictionary<string, string> replacements,
            WizardRunKind runKind,
            object[] customParams)
        {
            try
            {
                // Don't show the popup if the key has already been set.
                if (!replacements.ContainsKey(ReplacementsKeys.GcpProjectIdKey))
                {
                    GcpProject project = PromptPickProjectId();

                    if (project == null)
                    {
                        // Null indicates a canceled operation.
                        throw new WizardBackoutException();
                    }
                    replacements.Add(ReplacementsKeys.GcpProjectIdKey, project.ProjectId ?? "");
                }
                if (!replacements.ContainsKey(ReplacementsKeys.PackagesPathKey))
                {
                    string solutionDirectory = replacements[ReplacementsKeys.SolutionDirectoryKey];
                    string projectDirectory = replacements[ReplacementsKeys.DestinationDirectoryKey];
                    string packageDirectory = Path.Combine(solutionDirectory, "packages");
                    string packagesPath = PathUtils.GetRelativePath(projectDirectory, packageDirectory);
                    replacements.Add(ReplacementsKeys.PackagesPathKey, packagesPath);
                }
                replacements[ReplacementsKeys.EmbeddableSafeProjectNameKey] =
                    replacements[ReplacementsKeys.SafeProjectNameKey];
            }
            catch
            {
                CleanupDirectories(replacements);
                throw;
            }
        }

        ///<inheritdoc />
        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
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
        public void ProjectFinishedGenerating(VsProject project)
        {
        }
    }
}
