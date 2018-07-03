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

using System.Collections.Generic;

namespace GoogleCloudExtension.Deployment
{
    /// <summary>
    /// Implementation of this interface provide the path to the tools needed by the publish
    /// operations.
    /// </summary>
    public interface IToolsPathProvider
    {
        /// <summary>
        /// Returns the path to the directory where the tools used for publishing ASP.NET Core apps
        /// are stored. All tools are stored in the same directory.
        /// </summary>
        string GetExternalToolsPath();

        /// <summary>
        /// Returns the path to the dotnet.exe tool to use during deployment.
        /// </summary>
        string GetDotnetPath();

        /// <summary>
        /// Retruns the path to the msbuild.exe to use for builds.
        /// </summary>
        string GetMsbuildPath();

        /// <summary>
        /// Returns the path to Visual Studio Remote Debugger tools.
        /// </summary>
        string GetRemoteDebuggerToolsPath();

        /// <summary>
        /// Returns the .NET Core Sdk versions installed on this computer.
        /// </summary>
        IEnumerable<string> GetNetCoreSdkVersions();
    }
}
