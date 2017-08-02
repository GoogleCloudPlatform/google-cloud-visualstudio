﻿// Copyright 2017 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.Utils;
using System;
using System.IO;

namespace GoogleCloudExtension.VsVersion.VS15
{
    /// <summary>
    /// The implementation of <seealso cref="IToolsPathProvider"/> for Visual Studio 2017 (v 15.0).
    /// </summary>
    internal class ToolsPathProvider : IToolsPathProvider
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
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var result = Path.Combine(programFilesPath, $@"Microsoft Visual Studio\2017\{_edition}\Web\External");
            GcpOutputWindow.OutputDebugLine($"External tools path: {result}");
            return result;
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

        public string GetRemoteDebuggerToolsPath()
        {
            var programFilesPath = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            // TODO: add x86 support later
            var result = Path.Combine(programFilesPath, $@"Microsoft Visual Studio\2017\{_edition}\Common7\IDE\Remote Debugger\x64\*");
            GcpOutputWindow.OutputDebugLine($"Program Files: {programFilesPath}");
            GcpOutputWindow.OutputDebugLine($"Debugger remote tools V15 path: {result}");
            return result;
        }
    }
}
