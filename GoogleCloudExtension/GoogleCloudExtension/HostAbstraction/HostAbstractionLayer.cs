using GoogleCloudExtension.Deployment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.HostAbstraction
{
    static class HostAbstractionLayer
    {
        private const string VisualStudio2015Version = "14.0";
        private const string VisualStudio2017Version = "15.0";

        private static readonly Lazy<IToolsPathProvider> s_toolsPathProvider = new Lazy<IToolsPathProvider>(GetTooslPathProvider);

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
    }
}
