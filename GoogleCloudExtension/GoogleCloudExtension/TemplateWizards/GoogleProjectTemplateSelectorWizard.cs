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
using GoogleCloudExtension.TemplateWizards.Dialogs.TemplateChooserDialog;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace GoogleCloudExtension.TemplateWizards
{
    /// <summary>
    /// Wizard for selecting a project template.
    /// </summary>
    [Export(typeof(IGoogleProjectTemplateSelectorWizard))]
    public class GoogleProjectTemplateSelectorWizard : IGoogleProjectTemplateSelectorWizard
    {
        // Mockable static methods for unit testing.
        internal Func<string, TemplateChooserViewModelResult> PromptUser = TemplateChooserWindow.PromptUser;
        internal Action<Dictionary<string, string>> CleanupDirectories = GoogleTemplateWizardHelper.CleanupDirectories;

        /// <inheritdoc/>
        public void RunStarted(
            object automationObject,
            Dictionary<string, string> replacements,
            WizardRunKind runKind,
            object[] customParams)
        {
            try
            {
                string projectName = replacements[ReplacementsKeys.ProjectNameKey];

                TemplateChooserViewModelResult result;
                // Enable shortcutting the ui for functional testing.
                if (replacements.ContainsKey(ReplacementsKeys.TemplateChooserResultKey))
                {
                    result = JsonConvert.DeserializeObject<TemplateChooserViewModelResult>(
                        replacements[ReplacementsKeys.TemplateChooserResultKey]);
                }
                else
                {
                    result = PromptUser(projectName);
                }
                if (result == null)
                {
                    throw new WizardBackoutException();
                }
                string thisTemplateDirectory =
                    Path.GetDirectoryName(
                        Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName((string)customParams[0]))));


                var serviceProvider = automationObject as IServiceProvider;
                object[] newCustomParams = GetNewCustomParams(replacements, customParams.Skip(1), result);

                string version = result.SelectedVersion.Version;
                string templatePath = Path.Combine(
                    thisTemplateDirectory, result.AppType.ToString(), "1033", version, $"{version}.vstemplate");
                string destinationFolder = replacements[ReplacementsKeys.DestinationDirectoryKey];

                var vsSolution = (IVsSolution6)serviceProvider.QueryService<SVsSolution>();
                IVsHierarchy newProject;
                Marshal.ThrowExceptionForHR(
                    vsSolution.AddNewProjectFromTemplate(
                        templatePath, newCustomParams, null,
                        destinationFolder, projectName, null,
                        out newProject));
            }
            catch
            {
                CleanupDirectories(replacements);
                throw;
            }
            // Delegated wizard created the solution. Cancel repeated creation of the solution.
            throw new WizardCancelledException();
        }

        /// <inheritdoc/>
        public void ProjectFinishedGenerating(Project project)
        {
            throw new NotImplementedException("This wizard should delegate to another template and cancel itself.");
        }

        /// <inheritdoc/>
        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
            throw new NotImplementedException("This wizard should delegate to another template and cancel itself.");
        }

        /// <inheritdoc/>
        public bool ShouldAddProjectItem(string filePath)
        {
            throw new NotImplementedException("This wizard should delegate to another template and cancel itself.");
        }

        /// <inheritdoc/>
        public void BeforeOpeningFile(ProjectItem projectItem)
        {
            throw new NotImplementedException("This wizard should delegate to another template and cancel itself.");
        }

        /// <inheritdoc/>
        public void RunFinished()
        {
            throw new NotImplementedException("This wizard should delegate to another wizard and cancel itself.");
        }

        private static object[] GetNewCustomParams(
            IReadOnlyDictionary<string, string> replacements,
            IEnumerable customParams,
            TemplateChooserViewModelResult result)
        {
            string solutionDirectory = replacements[ReplacementsKeys.SolutionDirectoryKey];
            string projectDirectory = replacements[ReplacementsKeys.DestinationDirectoryKey];
            string packageDirectory = Path.Combine(solutionDirectory, "packages");
            string relativePackagesPath = PathUtils.GetRelativePath(projectDirectory, packageDirectory);

            var additionalCustomParams = new List<string>
            {
                $"{ReplacementsKeys.GcpProjectIdKey}={result.GcpProjectId}",
                $"{ReplacementsKeys.SolutionDirectoryKey}={solutionDirectory}",
                $"{ReplacementsKeys.PackagesPathKey}={relativePackagesPath}",
            };

            // TODO(jimwp): Find the latest framework version using IVsFrameworkMultiTargeting.
            if (result.SelectedVersion.IsCore)
            {
                switch (result.SelectedFramework)
                {
                    case FrameworkType.NetFramework:
                        var targetFramework = "net461";
                        additionalCustomParams.Add($"netcoreapp{result.SelectedVersion.Version}={targetFramework}");
                        break;
                    case FrameworkType.NetCore:
                        // Do nothing.
                        break;
                    case FrameworkType.None:
                        throw new InvalidOperationException(
                            $"{nameof(result.SelectedFramework)} must be one either " +
                            $"{FrameworkType.NetCore} or {FrameworkType.NetFramework}.");
                    default:
                        throw new InvalidOperationException(
                            $"Invalid {nameof(FrameworkType)}: {result.SelectedFramework}");
                }
            }
            return customParams.Cast<string>().Concat(additionalCustomParams).ToArray<object>();
        }
    }
}
