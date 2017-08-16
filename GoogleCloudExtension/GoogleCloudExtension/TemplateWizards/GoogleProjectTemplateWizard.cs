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
using GoogleCloudExtension.Deployment;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using MsBuildProject = Microsoft.Build.Evaluation.Project;

namespace GoogleCloudExtension.TemplateWizards
{
    /// <summary>
    /// Wizard for a project template.
    /// </summary>
    [Export(typeof(IGoogleProjectTemplateWizard))]
    public class GoogleProjectTemplateWizard : IGoogleProjectTemplateWizard
    {
        internal const string TargetFrameworkMoniker = "TargetFrameworkMoniker";
        internal const string SupportedTargetFrameworkItemName = "SupportedTargetFramework";
        internal const string GlobalJsonFileName = "global.json";
        internal const string DefaultFrameworkVersion = "4.5.2";
        internal const string FrameworkVersionKey = "$frameworkversion$";

        // Mockable static methods for testing.
        internal Func<string> PromptPickProjectId = PickProjectIdWindow.PromptUser;
        internal Action<string, bool> DeleteDirectory = Directory.Delete;
        internal IProjectParser Parser = ProjectParser.Instance;
        private string _oldWorkingDirectory;
        internal Func<string, MsBuildProject> GetMsBuildProject = projectName => new MsBuildProject(projectName);
        private DTE2 Dte { get; set; }

        private IServiceProvider ServiceProvider => (IServiceProvider)Dte;

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
            var frameworkService =
                (IVsFrameworkMultiTargeting)ServiceProvider.QueryService<SVsFrameworkMultiTargeting>();
            FrameworkName framework = GetSupportedFrameworks(frameworkService).Select(s => new FrameworkName(s))
                .Where(f => f.Identifier.Equals(".NETFramework") && string.IsNullOrEmpty(f.Profile))
                .OrderBy(f => f.Version).LastOrDefault();
            string frameworkVersion = framework?.Version.ToString() ?? DefaultFrameworkVersion;
            replacementsDictionary.Add(FrameworkVersionKey, "v" + frameworkVersion);
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
            if (GoogleCloudExtensionPackage.VsVersion == VsVersionUtils.VisualStudio2015Version ||
                Parser.ParseProject(project).ProjectType == KnownProjectTypes.WebApplication)
            {
                // VS 2015 style .NET Core project report the wrong framework type. Don't update it.
                // .Net Framework projects fail to load when updating the framework version in a template wizard.
                return;
            }
            string currentFrameworkFullName = project.Properties.Item(TargetFrameworkMoniker)?.Value?.ToString();
            if (currentFrameworkFullName != null)
            {
                var currentFramework = new FrameworkName(currentFrameworkFullName);
                MsBuildProject buildProject = GetMsBuildProject(project.FullName);

                IEnumerable<string> supportedFrameworks = buildProject.GetItems(SupportedTargetFrameworkItemName)
                    .Select(item => item.EvaluatedInclude);
                FrameworkName newestFramework = supportedFrameworks
                    .Select(s => new FrameworkName(s))
                    .Where(fn =>
                        fn.Identifier.Equals(currentFramework.Identifier, StringComparison.OrdinalIgnoreCase) &&
                        fn.Profile.Equals(currentFramework.Profile, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(fn => fn.Version)
                    .LastOrDefault();

                string newestFrameworkFullName = newestFramework?.FullName;
                if (!string.IsNullOrWhiteSpace(newestFrameworkFullName) &&
                    newestFrameworkFullName != currentFrameworkFullName)
                {
                    project.Properties.Item(TargetFrameworkMoniker).Value = newestFrameworkFullName;
                }
            }
        }

        private static IList<string> GetSupportedFrameworks(IVsFrameworkMultiTargeting frameworkService)
        {
            Array supportedFrameworks;
            Marshal.ThrowExceptionForHR(frameworkService.GetSupportedFrameworks(out supportedFrameworks));
            return (string[])supportedFrameworks;
        }
    }
}
