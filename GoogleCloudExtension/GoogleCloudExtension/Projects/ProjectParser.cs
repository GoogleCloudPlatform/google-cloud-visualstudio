using EnvDTE;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GoogleCloudExtension.Projects
{
    internal static class ProjectParser
    {
        // Identifiers of an ASP.NET 4.x .csproj
        private const string MsbuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";
        private const string WebApplicationGuid = "{349c5851-65df-11da-9384-00065b846f21}";

        // Identifier of an ASP.NET Core 1.x .csproj
        private const string AspNetCoreSdk = "Microsoft.NET.Sdk.Web";

        // Extension used in .NET Core 1.0 project placeholders, the real project is in project.json.
        private const string XProjExtension = ".xproj";
        private const string ProjectJsonFileName = "project.json";

        // Extension used in .NET Core 1.0, 1.1... and .NET 4.x, etc...
        private const string CSProjExtension = ".csproj";

        /// <summary>
        /// Parses the given <seealso cref="Project"/> instance and returned a friendlier and more usable type to use for
        /// deplyment and other operations.
        /// </summary>
        /// <param name="project">The <seealso cref="Project"/> instance to parse.</param>
        /// <returns>The resulting <seealso cref="IParsedProject"/> or null if the project is not supported.</returns>
        public static IParsedProject ParseProject(Project project)
        {
            var extension = Path.GetExtension(project.FullName);
            switch (extension)
            {
                case XProjExtension:
                    Debug.WriteLine($"Processing a project.json: {project.FullName}");
                    return ParseProjectJson(project);

                case CSProjExtension:
                    Debug.WriteLine($"Processing a .csproj: {project.FullName}");
                    return ParseMsbuildProject(project);

                default:
                    return null;
            }
        }

        private static IParsedProject ParseMsbuildProject(Project project)
        {
            GcpOutputWindow.OutputDebugLine($"Parsing .csproj {project.FullName}");

            var dom = XDocument.Load(project.FullName);
            var sdk = dom.Root.Attribute("Sdk");
            if (sdk != null)
            {
                GcpOutputWindow.OutputDebugLine($"Found a new style .csproj {sdk.Value}");
                if (sdk.Value == AspNetCoreSdk)
                {
                    var targetFramework = dom.Root
                        .Elements("PropertyGroup")
                        .Descendants("TargetFramework")
                        .Select(x => x.Value)
                        .FirstOrDefault();
                    return new NetCoreCsprojProject(project, targetFramework);
                }
            }

            var projectGuids = dom.Root
                .Elements(XName.Get("PropertyGroup", MsbuildNamespace))
                .Descendants(XName.Get("ProjectTypeGuids", MsbuildNamespace))
                .Select(x => x.Value)
                .FirstOrDefault();

            if (projectGuids == null)
            {
                return null;
            }

            var guids = projectGuids.Split(';');
            if (guids.Contains(WebApplicationGuid))
            {
                return new NetCsprojProject(project);
            }
            return null;
        }

        private static IParsedProject ParseProjectJson(Project project)
        {
            var projectDir = Path.GetDirectoryName(project.FullName);
            var projectJsonPath = Path.Combine(projectDir, ProjectJsonFileName);

            if (!File.Exists(projectJsonPath))
            {
                Debug.WriteLine($"Could not find {projectJsonPath}.");
                return null;
            }

            return new NetCoreJsonProject(projectJsonPath);
        }
    }
}
