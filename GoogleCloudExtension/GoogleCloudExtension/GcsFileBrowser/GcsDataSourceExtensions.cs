using GoogleCloudExtension.DataSources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public static async Task CreateDirectoryAsync(this GcsDataSource self, string bucket, string prefix)
        {
            await self.UploadStreamAsync(
                bucket: bucket,
                name: prefix,
                stream: Stream.Null,
                contentType: "application/x-www-form-urlencoded;charset=UTF-8");
        }

        private static string GetNameLeaf(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                return name;
            }

            var cleanName = name.Substring(0, name.Length - 1);
            return cleanName.Split('/').LastOrDefault() ?? "";
        }
    }
}
