using Google.Apis.Storage.v1.Data;
using GoogleCloudExtension.Utils;
using System.Linq;

namespace GoogleCloudExtension.GcsFileBrowser
{
    public class GcsItem
    {
        public string Name { get; }

        public string FileName { get; }

        public bool IsDirectory { get; }

        public ulong Size { get; }

        public string LastModified { get; }

        public GcsItem(string name)
        {
            Name = name;
            IsDirectory = true;
            FileName = GetLeafName(Name);
        }

        public GcsItem(Object obj)
        {
            Name = obj.Name;
            IsDirectory = false;
            Size = obj.Size ?? 0;
            LastModified = obj.Updated?.ToString() ?? "Unknown";
            FileName = GetLeafName(Name);
        }

        private static string GetLeafName(string name)
        {
            var cleanName = name.Last() == '/' ? name.Substring(0, name.Length - 1) : name;
            return cleanName.Split('/').Last();
        }
    }
}