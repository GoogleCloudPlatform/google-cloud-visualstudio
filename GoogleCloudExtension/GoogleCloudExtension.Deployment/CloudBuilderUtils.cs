using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Deployment
{
    internal static class CloudBuilderUtils
    {
        private const string CloudBuildFileContent =
           "steps:\n" +
            "- name: gcr.io/cloud-builders/docker\n" +
            "  args: [ 'build', '-t', '{0}', '--no-cache', '--pull', '.' ]\n" +
            "images:\n" +
            "  ['{0}']\n";

        internal static string CreateBuildFile(string project, string imageName, string imageVersion, string buildFilePath)
        {
            var tag = GetDeploymentTag(
                project: project,
                imageName: imageName,
                imageVersion: imageVersion);
            Debug.WriteLine($"Creating build file for tag {tag} at {buildFilePath}");
            var content = String.Format(CloudBuildFileContent, tag);
            File.WriteAllText(buildFilePath, content);
            return tag;
        }

        private static void Cleanup(string buildFilePath)
        {
            try
            {
                if (File.Exists(buildFilePath))
                {
                    File.Delete(buildFilePath);
                }
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Failed to cleanup file {buildFilePath}");
            }
        }

        private static string GetDeploymentTag(string project, string imageName, string imageVersion)
            => $"gcr.io/{project}/{imageName}:{imageVersion}";
    }
}
