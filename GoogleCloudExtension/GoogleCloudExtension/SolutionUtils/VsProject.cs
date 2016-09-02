using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.SolutionUtils
{
    /// <summary>
    /// This clas represents a native Visual Studio project.
    /// </summary>
    internal class VsProject : ISolutionProject
    {
        private readonly Project _project;

        public string DirectoryPath => Path.GetDirectoryName(_project.FullName);

        public string FullPath => _project.FullName;

        public string Name => _project.Name;

        public KnownProjectTypes ProjectType => _project.GetProjectType();

        public VsProject(Project project)
        {
            _project = project;
        }
    }
}
