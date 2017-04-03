using EnvDTE;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GoogleCloudExtension.Projects
{
    /// <summary>
    /// This class represents a .NET Core project based on .csproj.
    /// </summary>
    internal class NetCoreCsprojProject : IParsedProject
    {
        private readonly Project _project;

        #region IParsedProject

        public string DirectoryPath => Path.GetDirectoryName(_project.FullName);

        public string FullPath => _project.FullName;

        public string Name => _project.Name;

        public KnownProjectTypes ProjectType { get; }

        #endregion

        public NetCoreCsprojProject(Project project, string targetFramework)
        {
            GcpOutputWindow.OutputDebugLine($"Found project {project.FullName} targeting {targetFramework}");

            _project = project;
            switch (targetFramework)
            {
                case "netcoreapp1.0":
                    ProjectType = KnownProjectTypes.NetCoreWebApplication1_0;
                    break;

                case "netcoreapp1.1":
                    ProjectType = KnownProjectTypes.NetCoreWebApplication1_1;
                    break;

                default:
                    GcpOutputWindow.OutputDebugLine($"Unsopported target framework {targetFramework}");
                    ProjectType = KnownProjectTypes.None;
                    break;
            }
        }
    }
}
