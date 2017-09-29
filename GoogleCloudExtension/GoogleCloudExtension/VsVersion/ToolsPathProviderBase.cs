using GoogleCloudExtension.Deployment;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GoogleCloudExtension.VsVersion
{
    internal abstract class ToolsPathProviderBase : IToolsPathProvider
    {
        internal const string SdkDirectoryName = "sdk";
        internal const string NugetFallbackFolderName = "NuGetFallbackFolder";

        /// <inheritdoc />
        public abstract string GetExternalToolsPath();

        /// <inheritdoc />
        public abstract string GetDotnetPath();

        /// <inheritdoc />
        public abstract string GetMsbuildPath();

        /// <inheritdoc />
        public abstract string GetMsdeployPath();

        /// <inheritdoc />
        public abstract string GetRemoteDebuggerToolsPath();

        /// <inheritdoc />
        public IEnumerable<string> GetNetCoreSdkVersions()
        {
            string dotnetDir = Path.GetDirectoryName(GetDotnetPath());
            if (dotnetDir == null)
            {
                return Enumerable.Empty<string>();
            }
            string sdkDirectoryPath = Path.Combine(dotnetDir, SdkDirectoryName);
            if (!Directory.Exists(sdkDirectoryPath))
            {
                return Enumerable.Empty<string>();
            }
            Version dummy;
            return Directory.EnumerateDirectories(sdkDirectoryPath)
                .Select(Path.GetFileName)
                .Where(
                    sdkVersion =>
                        sdkVersion.StartsWith("1.0.0-preview") || Version.TryParse(sdkVersion, out dummy));
        }
    }
}