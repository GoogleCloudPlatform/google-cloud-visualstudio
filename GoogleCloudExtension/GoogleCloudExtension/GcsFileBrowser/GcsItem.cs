using Google.Apis.Storage.v1.Data;
using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.GcsFileBrowser
{
    public class GcsItem
    {
        public string Name { get; }

        public bool IsDirectory { get; }

        public ulong Size { get; }

        public string LastModified { get; }

        public GcsItem(string name)
        {
            Name = name;
            IsDirectory = true;
        }

        public GcsItem(Object obj)
        {
            Name = obj.Name;
            IsDirectory = false;
            Size = obj.Size ?? 0;
            LastModified = obj.Updated?.ToString() ?? "Unknown";
        }
    }
}