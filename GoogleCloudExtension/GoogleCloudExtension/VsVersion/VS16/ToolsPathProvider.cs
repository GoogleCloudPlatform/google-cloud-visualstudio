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

using System.IO;

namespace GoogleCloudExtension.VsVersion.VS16
{
    /// <summary>
    /// The implementation of <seealso cref="Deployment.IToolsPathProvider"/> for Visual Studio 2019 (v16.0).
    /// </summary>
    internal class ToolsPathProvider : ToolsPathProviderBase
    {
        public const string MSBuildSubPath = @"MSBuild\Current\Bin\MSBuild.exe";

        public override string GetMsbuildPath()
        {
            string result = Path.Combine(VsRootDirectoryPath, MSBuildSubPath);
            GcpOutputWindow.Default.OutputDebugLine($"Msbuild V16 Path: {result}");
            return result;
        }
    }
}
