// Copyright 2016 Google Inc. All Rights Reserved.
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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
namespace GoogleCloudExtension.SolutionUtils
{
    internal class ProjectHelper
    {
        private const string AssemblyVersionProperty = "AssemblyVersion";
        private const string AssemblyNameProperty = "AssemblyName";

        private readonly Project _project;

        public string Version { get; private set; }
        public string AssemblyName { get; private set; }

        private ProjectHelper(Project project)
        {
            if (project == null)
            {
                throw new ArgumentNullException($"Input parameter {nameof(project)} is null.");
            }

            _project = project;
            ParseProperties();
        }


        /// <summary>
        /// Create a <seealso cref="ProjectHelper"/> object wrapping up a Project interface.
        /// Together with private constructor, this ensures object creation won't run into exception. 
        /// </summary>
        /// <param name="project">Project interface.</param>
        /// <returns>
        /// The created object.
        /// Or null if the project is null, or not supported project type.
        /// </returns>
        public static ProjectHelper Create(Project project)
        {
            if (!IsValidSupported(project))
            {
                return null;
            }

            return new ProjectHelper(project);
        }

        private static bool IsValidSupported(Project project)
        {
            return project != null; //  TODO: verify project is c# projects. && project.Kind
        }

        private void ParseProperties()
        {
            foreach (Property property in _project.Properties)
            {
                switch (property.Name)
                {
                    case AssemblyNameProperty:
                        AssemblyName = property.Value as string;
                        break;
                    case AssemblyVersionProperty:
                        Version = property.Value as string;
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
