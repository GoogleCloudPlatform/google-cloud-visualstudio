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

using System;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.VsVersion.VS14;

namespace GoogleCloudExtension.VsVersion
{
    /// <summary>
    /// This class provides services that are Visual Studio version specific.
    /// </summary>
    internal static class VsVersionUtils
    {
        public const string VisualStudio2015Version = "14.0";
        public const string VisualStudio2017Version = "15.0";
        public const string VisualStudio2019Version = "16.0";
        public const int Vs2015DebuggerPort = 4020;
        public const int Vs2017DebuggerPort = 4022;
        public const int Vs2019DebuggerPort = 4024;

        private static readonly Lazy<IToolsPathProvider> s_toolsPathProvider =
            new Lazy<IToolsPathProvider>(GetToolsPathProvider);
        private static readonly Lazy<int> s_remoteDebuggerPort = new Lazy<int>(GetRemoteDebuggerPort);
        internal static IToolsPathProvider s_toolsPathProviderOverride = null;

        /// <summary>
        /// The remote debugger tool port number.
        /// </summary>
        public static int RemoteDebuggerPort => s_remoteDebuggerPort.Value;

        /// <summary>
        /// The instance of <seealso cref="IToolsPathProvider"/> to use for this version of Visual Studio.
        /// </summary>
        public static IToolsPathProvider ToolsPathProvider => s_toolsPathProviderOverride ?? s_toolsPathProvider.Value;

        public static IToolsPathProvider GetToolsPathProvider()
        {
            switch (GoogleCloudExtensionPackage.Instance.VsVersion)
            {
                case VisualStudio2015Version:
                    return new ToolsPathProvider();
                case VisualStudio2017Version:
                    return new VS15.ToolsPathProvider();
                case VisualStudio2019Version:
                    return new VS16.ToolsPathProvider();
                default:
                    throw new NotSupportedException($"Version {GoogleCloudExtensionPackage.Instance.VsVersion} is not supported.");
            }
        }

        internal static int GetRemoteDebuggerPort()
        {
            switch (GoogleCloudExtensionPackage.Instance.VsVersion)
            {
                case VisualStudio2015Version:
                    return Vs2015DebuggerPort;
                case VisualStudio2017Version:
                    return Vs2017DebuggerPort;
                case VisualStudio2019Version:
                    return Vs2019DebuggerPort;
                default:
                    throw new NotSupportedException($"Version {GoogleCloudExtensionPackage.Instance.VsVersion} is not supported.");
            }
        }
    }
}
