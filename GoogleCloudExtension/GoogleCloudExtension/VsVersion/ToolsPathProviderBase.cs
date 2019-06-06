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

using EnvDTE80;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Services.FileSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GoogleCloudExtension.VsVersion
{
    public abstract class ToolsPathProviderBase : IToolsPathProvider
    {
        private const string SdkDirectoryName = "sdk";
        public const string NugetFallbackFolderName = "NuGetFallbackFolder";
        public const string DotnetExeSubPath = @"dotnet\dotnet.exe";
        public const string ProgramW6432 = "%ProgramW6432%";

        private readonly Lazy<IFileSystem> _fileSystem =
            GoogleCloudExtensionPackage.Instance.GetMefServiceLazy<IFileSystem>();

        private readonly Lazy<IEnvironment> _environment =
            GoogleCloudExtensionPackage.Instance.GetMefServiceLazy<IEnvironment>();

        /// <summary>
        /// The <see cref="IEnvironment"/> service.
        /// </summary>
        protected IEnvironment Environment => _environment.Value;

        /// <summary>
        /// The root directory of the current Visual Studio installation.
        /// </summary>
        protected string VsRootDirectoryPath
        {
            get
            {
                string devenvPath = Dte.FullName;
                string ideDirectoryPath = Path.GetDirectoryName(devenvPath);
                string common7DirectoryPath = Path.GetDirectoryName(ideDirectoryPath);
                string vsRootDirectoryPath = Path.GetDirectoryName(common7DirectoryPath);
                Debug.Assert(vsRootDirectoryPath != null);
                return vsRootDirectoryPath;
            }
        }

        private IFileSystem FileSystem => _fileSystem.Value;
        private DTE2 Dte => GoogleCloudExtensionPackage.Instance.Dte;

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
            string ideDirectoryPath = Path.GetDirectoryName(devenvPath);
            Debug.Assert(ideDirectoryPath != null);
            // TODO: add x86 support later
            return Path.Combine(ideDirectoryPath, "Remote Debugger", "x64", "*");
        }

        public string GetExternalToolsPath() => Path.Combine(VsRootDirectoryPath, "Web", "External");

        public string GetDotnetPath()
        {
            string programFilesPath = Environment.ExpandEnvironmentVariables(ProgramW6432);
            return Path.Combine(programFilesPath, DotnetExeSubPath);
        }
    }
}