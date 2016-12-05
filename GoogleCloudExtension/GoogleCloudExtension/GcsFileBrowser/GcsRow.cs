using Google.Apis.Storage.v1.Data;
using System.Linq;

namespace GoogleCloudExtension.GcsFileBrowser
{
    public class GcsRow
    {
        public string Bucket { get; private set; }

        public string Name { get; private set; }

        public string FileName { get; private set; }

        public bool IsError { get; private set; }

        public bool IsFile { get; private set; }

        public bool IsDirectory { get; private set; }

        public ulong Size { get; private set; }

        public string LastModified { get; private set; }

        public GcsRow() { }

        public static GcsRow CreateDirectoryRow(string name) =>
            new GcsRow
            {
                Name = name,
                FileName = GetLeafName(name),
                IsDirectory = true,
            };

        public static GcsRow CreateFileRow(Object obj) =>
            new GcsRow
            {
                Bucket = obj.Bucket,
                Name = obj.Name,
                IsFile = true,
                Size = obj.Size.HasValue ? obj.Size.Value : 0ul,
                LastModified = obj.Updated?.ToString() ?? "Unknown",
                FileName = GetLeafName(obj.Name),
            };

        public static GcsRow CreateErrorRow(string message) =>
            new GcsRow
            {
                FileName = message,
                IsError = true
            };

        private static string GetLeafName(string name)
        {
            var cleanName = name.Last() == '/' ? name.Substring(0, name.Length - 1) : name;
            return cleanName.Split('/').Last();
        }
    }
}