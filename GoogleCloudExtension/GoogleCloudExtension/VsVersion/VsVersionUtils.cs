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

using GoogleCloudExtension.Deployment;
using System;

namespace GoogleCloudExtension.VsVersion
{
    /// <summary>
    /// This class provides services that are Visual Studio version specific.
    /// </summary>
    internal static class VsVersionUtils
    {
        public const string VisualStudio2015Version = "14.0";
        public const string VisualStudio2017Version = "15.0";

        internal static Lazy<IToolsPathProvider> s_toolsPathProvider =
            new Lazy<IToolsPathProvider>(GetTooslPathProvider);
        private static readonly Lazy<int> s_remoteDebuggerPort = new Lazy<int>(GetRemoteDebuggerPort);

        /// <summary>
        /// The remote debugger tool port number.
        /// </summary>
        public static int RemoteDebuggerPort => s_remoteDebuggerPort.Value;

        /// <summary>
        /// The instance of <seealso cref="IToolsPathProvider"/> to use for this version of Visual Studio.
        /// </summary>
        public static IToolsPathProvider ToolsPathProvider => s_toolsPathProvider.Value;

        private static IToolsPathProvider GetTooslPathProvider()
        {
            switch (GoogleCloudExtensionPackage.VsVersion)
            {
                case VisualStudio2015Version:
                    return new VS14.ToolsPathProvider();

                case VisualStudio2017Version:
                    return new VS15.ToolsPathProvider(GoogleCloudExtensionPackage.VsEdition);

                default:
                    throw new NotSupportedException($"Version {GoogleCloudExtensionPackage.VsVersion} is not supported.");
            }
        }

        private static int GetRemoteDebuggerPort()
        {
            switch (GoogleCloudExtensionPackage.VsVersion)
            {
                case VisualStudio2015Version:
                    return 4020;

                case VisualStudio2017Version:
                    return 4022;

                default:
                    throw new NotSupportedException($"Version {GoogleCloudExtensionPackage.VsVersion} is not supported.");
            }
        }
    }
}
