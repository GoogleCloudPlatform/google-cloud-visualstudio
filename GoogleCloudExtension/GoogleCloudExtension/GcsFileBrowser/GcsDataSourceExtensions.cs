using GoogleCloudExtension.DataSources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GcsFileBrowser
{
    internal class GcsFileReference
    {
        public string Bucket { get; }

        public string Name { get; }

        public string RelativeName { get; }

        public GcsFileReference(string bucket, string name, string prefix)
        {
            Bucket = bucket;
            Name = name;
            RelativeName = GetRelativeName(name, prefix);
        }

        private static string GetRelativeName(string fullName, string prefix)
        {
            if (fullName.StartsWith(prefix))
            {
                return fullName.Substring(prefix.Length);
            }
            else
            {
                return fullName;
            }
        }
    }

    internal static class GcsDataSourceExtensions
    {
        public static async Task<IEnumerable<GcsFileReference>> GetGcsFilesFromPrefixAsync(
            this GcsDataSource self,
            string bucket,
            string prefix)
        {
            var files = await self.GetObjectLisAsync(bucket, prefix);
            return files.Select(x => new GcsFileReference(bucket, x.Name, prefix)).ToList();
        }

        public static async Task<IEnumerable<GcsFileReference>> GetGcsFilesFromPrefixesAsync(
            this GcsDataSource self,
            string bucket,
            IEnumerable<string> prefixes)
        {
            var result = new List<GcsFileReference>();
            foreach (var prefix in prefixes)
            {
                result.AddRange(await self.GetGcsFilesFromPrefixAsync(bucket, prefix));
            }
            return result;
        }
    }
}
