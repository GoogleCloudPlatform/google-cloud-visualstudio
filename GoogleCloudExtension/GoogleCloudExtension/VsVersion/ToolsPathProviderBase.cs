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
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Services.FileSystem;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GoogleCloudExtension.VsVersion
{
    public abstract class ToolsPathProviderBase : IToolsPathProvider
    {
        public const string SdkDirectoryName = "sdk";
        public const string NugetFallbackFolderName = "NuGetFallbackFolder";
        public const string DotnetExeSubPath = @"dotnet\dotnet.exe";
        public const string ProgramW6432 = "%ProgramW6432%";

        private readonly Lazy<IFileSystem> _fileSystem =
            GoogleCloudExtensionPackage.Instance.GetMefServiceLazy<IFileSystem>();

        private readonly Lazy<IEnvironment> _environment =
            GoogleCloudExtensionPackage.Instance.GetMefServiceLazy<IEnvironment>();

        private readonly Lazy<DTE> _dte = new Lazy<DTE>(GoogleCloudExtensionPackage.Instance.GetService<SDTE, DTE>);

        private IFileSystem FileSystem => _fileSystem.Value;
        protected IEnvironment Environment => _environment.Value;
        protected DTE Dte => _dte.Value;

        /// <inheritdoc />
        public abstract string GetMsbuildPath();

        /// <inheritdoc />
        public IEnumerable<string> GetNetCoreSdkVersions()
        {
            string dotnetDir = Path.GetDirectoryName(GetDotnetPath());
            Debug.Assert(dotnetDir != null);
            string sdkDirectoryPath = Path.Combine(dotnetDir, SdkDirectoryName);
            if (!FileSystem.Directory.Exists(sdkDirectoryPath))
            {
                return Enumerable.Empty<string>();
            }
            Version dummy;
            return FileSystem.Directory.EnumerateDirectories(sdkDirectoryPath)
                .Select(Path.GetFileName)
                .Where(
                    sdkVersion =>
                        sdkVersion.StartsWith("1.0.0-preview", StringComparison.Ordinal) ||
                        Version.TryParse(sdkVersion, out dummy));
        }

        public string GetRemoteDebuggerToolsPath()
        {
            string devenvPath = Dte.FullName;
            string ideDirctoryPath = Path.GetDirectoryName(devenvPath);
            Debug.Assert(ideDirctoryPath != null);
            // TODO: add x86 support later
            string result = Path.Combine(ideDirctoryPath, "Remote Debugger", "x64", "*");
            GcpOutputWindow.Default.OutputDebugLine($"Debugger remote tools path: {result}");
            return result;
        }

        public string GetExternalToolsPath()
        {
            string devenvPath = Dte.FullName;
            string ideDirectoryPath = Path.GetDirectoryName(devenvPath);
            string common7DirectoryPath = Path.GetDirectoryName(ideDirectoryPath);
            string vsRootDirectoryPath = Path.GetDirectoryName(common7DirectoryPath);
            Debug.Assert(vsRootDirectoryPath != null);
            string result = Path.Combine(vsRootDirectoryPath, "Web", "External");
            GcpOutputWindow.Default.OutputDebugLine($"External tools path: {result}");
            return result;
        }

        public string GetDotnetPath()
        {
            string programFilesPath = Environment.ExpandEnvironmentVariables(ProgramW6432);
            string result = Path.Combine(programFilesPath, DotnetExeSubPath);
            GcpOutputWindow.Default.OutputDebugLine($"Dotnet path: {result}");
            return result;
        }
    }
}