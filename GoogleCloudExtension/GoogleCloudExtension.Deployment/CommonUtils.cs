using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
