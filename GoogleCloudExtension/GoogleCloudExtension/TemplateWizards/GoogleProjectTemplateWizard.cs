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
using Microsoft.VisualStudio.TemplateWizard;
using NuGet.VisualStudio;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Task = System.Threading.Tasks.Task;

namespace GoogleCloudExtension.TemplateWizards
{
    /// <summary>
    /// Wizard for a project template.
    /// </summary>
    [Export(typeof(IGoogleProjectTemplateWizard))]
    public class GoogleProjectTemplateWizard : IGoogleProjectTemplateWizard
    {
        private IVsPackageInstaller2 _installer;

        private IEnumerable<PackageReference> _packages = Enumerable.Empty<PackageReference>();

        [ImportingConstructor]
        public GoogleProjectTemplateWizard(IVsPackageInstaller2 installer)
        {
            _installer = installer;
        }

        ///<inheritdoc />
        public void RunStarted(
            object automationObject,
            Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind,
            object[] customParams)
        {
            string vsTemplatePath = customParams.FirstOrDefault() as string;
            if (vsTemplatePath != null)
            {
                _packages = GetPackageRefernces(vsTemplatePath);
            }
            string projectId = PickProjectIdWindow.PromptUser();
            if (projectId == null)
            {
                throw new WizardBackoutException();
            }
            replacementsDictionary.Add("$gcpprojectid$", projectId);
            var solutionDir = new Uri(replacementsDictionary["$solutiondirectory$"]);
            var packageDir = new Uri(solutionDir, "packages/");
            var projectDir = new Uri(replacementsDictionary["$destinationdirectory$"] + "/");
            var packagesPath = projectDir.MakeRelativeUri(packageDir).ToString();
            replacementsDictionary.Add("$packagespath$", packagesPath.Replace('/', Path.DirectorySeparatorChar));
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
        public async void ProjectFinishedGenerating(Project project)
        {
            List<Task> allTasks = new List<Task>();
            foreach (PackageReference package in _packages)
            {
                allTasks.Add(InstallPackageAsync(project, package));
            }
            await Task.WhenAll(allTasks);
        }

        private async Task InstallPackageAsync(Project project, PackageReference package)
        {
            await Task.Run(() => _installer.InstallPackage(null, project, package.Id, package.Version, true));
        }

        private IEnumerable<PackageReference> GetPackageRefernces(string vsTemplatePath)
        {
            XDocument vsTemplate = XDocument.Load(vsTemplatePath);
            if (vsTemplate.Root == null)
            {
                throw new InvalidOperationException();
            }
            IEnumerable<XElement> dataElement =
                vsTemplate.Root.Elements().Where(e => e.Name.LocalName == "WizardData");
            IEnumerable<XElement> packagesElements =
                dataElement.Elements().Where(e => e.Name.LocalName == "packages");
            IEnumerable<XElement> packageElements =
                packagesElements.Elements().Where(e => e.Name.LocalName == "package");
            foreach (XElement packageElement in packageElements)
            {
                string id = packageElement.Attribute("id")?.Value;
                string version = packageElement.Attribute("version")?.Value;
                yield return new PackageReference(id, version);
            }
        }
    }

    internal class PackageReference
    {
        public string Id { get; }
        public Version Version { get; }

        public PackageReference(string id, string version)
        {
            Id = id;
            Version = Version.Parse(version);
        }
    }
}
