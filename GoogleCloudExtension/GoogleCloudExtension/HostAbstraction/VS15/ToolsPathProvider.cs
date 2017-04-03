using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.HostAbstraction.VS15
{
    class ToolsPathProvider : IToolsPathProvider
    {
        private readonly string _edition;

        public ToolsPathProvider(string edition)
        {
            _edition = edition;
        }

        public string GetDotnetPath()
        {
            var programFilesPath = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
            var result = Path.Combine(programFilesPath, @"dotnet\dotnet.exe");
            GcpOutputWindow.OutputDebugLine($"Dotnet path: {result}");
            return result;
        }

        public string GetExternalToolsPath()
        {
            return "";
        }

        public string GetMsbuildPath()
        {
            var programFilesPath = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            var result = Path.Combine(programFilesPath, $@"Microsoft Visual Studio\2017\{_edition}\MSBuild\15.0\Bin\MSBuild.exe");
            GcpOutputWindow.OutputDebugLine($"Program Files: {programFilesPath}");
            GcpOutputWindow.OutputDebugLine($"Msbuild V15 Path: {result}");
            return result;
        }

        public string GetMsdeployPath()
        {
            var programFilesPath = Environment.GetEnvironmentVariable("ProgramFiles");
            var result = Path.Combine(programFilesPath, @"IIS\Microsoft Web Deploy V3\msdeploy.exe");
            GcpOutputWindow.OutputDebugLine($"Program Files: {programFilesPath}");
            GcpOutputWindow.OutputDebugLine($"Msdeploy V15 path: {result}");
            return result;
        }
    }
}
