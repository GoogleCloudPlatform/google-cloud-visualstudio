using System.IO;

namespace GoogleCloudExtension.Deployment
{
    internal static class CommonUtils
    {
        internal static string GetProjectName(string projectPath)
        {
            var directory = Path.GetDirectoryName(projectPath);
            return Path.GetFileName(directory);
        }
    }
}
