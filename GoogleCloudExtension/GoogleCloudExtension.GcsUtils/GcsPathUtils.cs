using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GcsUtils
{
    public static class GcsPathUtils
    {
        /// <summary>
        /// Returns the leaf on a GCS path.
        /// </summary>
        /// <param name="name">The path within a bucket.</param>
        public static string GetFileName(string name)
        {
            var cleanName = name.Last() == '/' ? name.Substring(0, name.Length - 1) : name;
            return cleanName.Split('/').Last();
        }

        /// <summary>
        /// Safely append <paramref name="child"/> to <paramref name="baseDir"/> adding the path separator
        /// if needed.
        /// </summary>
        /// <param name="baseDir">The base directory for the new path.</param>
        /// <param name="child">The child path.</param>
        public static string Combine(string baseDir, string child)
        {
            StringBuilder result = new StringBuilder(baseDir);
            if (!String.IsNullOrEmpty(baseDir) && baseDir.Last() != '/')
            {
                result.Append('/');
            }
            result.Append(child);
            return result.ToString();
        }
    }
}
