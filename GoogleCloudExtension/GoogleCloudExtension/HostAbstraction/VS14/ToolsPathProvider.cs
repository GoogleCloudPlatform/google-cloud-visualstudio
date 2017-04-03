using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.HostAbstraction.VS14
{
    class ToolsPathProvider : IToolsPathProvider
    {
        public string GetExternalToolsPath()
        {
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var result = Path.Combine(programFilesPath, @"Microsoft Visual Studio 14.0\Web\External");
            GcpOutputWindow.OutputDebugLine($"External tools path: {result}");
            return result;
        }

        public string GetDotnetPath()
        {
            var programFilesPath = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
            var result = Path.Combine(programFilesPath, @"dotnet\dotnet.exe");
            GcpOutputWindow.OutputDebugLine($"Dotnet path: {result}");
            return result;
        }

        public string GetMsbuildPath()
        {
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var result = Path.Combine(programFilesPath, @"MSBuild\14.0\Bin\MSBuild.exe");
            GcpOutputWindow.OutputDebugLine($"Msbuild path: {result}");
            return result;
        }

        public string GetMsdeployPath()
        {
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var result = Path.Combine(programFilesPath, @"IIS\Microsoft Web Deploy V3\msdeploy.exe");
            GcpOutputWindow.OutputDebugLine($"Msdeploy path: {result}");
            return result;
        }
    }
}
