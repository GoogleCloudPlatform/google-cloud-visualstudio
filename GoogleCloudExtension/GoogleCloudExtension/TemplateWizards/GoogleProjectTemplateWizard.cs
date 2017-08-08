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
using EnvDTE80;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.Projects;
using GoogleCloudExtension.TemplateWizards.Dialogs.ProjectIdDialog;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.VsVersion;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

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
        private string _oldWorkingDirectory;
        private DTE2 Dte { get; set; }

        private IServiceProvider ServiceProvider => (IServiceProvider)Dte;

        private const string GlobalJsonFileName = "global.json";
        private const string TargetFrameworkMoniker = "TargetFrameworkMoniker";

        ///<inheritdoc />
        public void RunStarted(
            object automationObject,
            Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind,
            object[] customParams)
        {
            Dte = (DTE2)automationObject;
            bool isEmbedded = Dte.CommandLineArguments.Contains("-Embedding");

            // When running as an embedded process, don't show the popup.
            string projectId = isEmbedded ?
                CredentialsStore.Default.CurrentProjectId ?? "dummy-project" :
                PromptPickProjectId();

            if (projectId == null)
            {
                // Null indicates a canceled operation.
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
            // To perform an upgrade, VS needs to create a new Proj.xproj for some reason.
            // It was trying to do that in the devenv folder. Instead do it in the temp folder.
            _oldWorkingDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Path.GetTempPath());
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
            Directory.SetCurrentDirectory(_oldWorkingDirectory);
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
            if (GoogleCloudExtensionPackage.VsVersion == VsVersionUtils.VisualStudio2015Version &&
                ProjectParser.ParseProject(project).ProjectType == KnownProjectTypes.NetCoreWebApplication1_0)
            {
                // VS 2015 style .NET Core project report the wrong framework type. Don't update it.
                return;
            }
            string currentFrameworkString = project.Properties.Item(TargetFrameworkMoniker)?.Value?.ToString();
            if (currentFrameworkString != null)
            {
                var currentFramework = new FrameworkName(currentFrameworkString);
                var frameworkService =
                    (IVsFrameworkMultiTargeting)ServiceProvider.QueryService<SVsFrameworkMultiTargeting>();
                Array supportedFrameworks;
                Marshal.ThrowExceptionForHR(frameworkService.GetSupportedFrameworks(out supportedFrameworks));
                FrameworkName newestFramework = supportedFrameworks.Cast<string>()
                    .Select(s => new FrameworkName(s))
                    .Where(fn =>
                        fn.Identifier.Equals(currentFramework.Identifier, StringComparison.OrdinalIgnoreCase) &&
                        fn.Profile.Equals(currentFramework.Profile, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(fn => fn.Version)
                    .LastOrDefault();

                if (!string.IsNullOrWhiteSpace(newestFramework?.FullName))
                {
                    project.Properties.Item(TargetFrameworkMoniker).Value = newestFramework.FullName;
                    project.Save(project.FullName);
                }
            }
        }
    }
}
