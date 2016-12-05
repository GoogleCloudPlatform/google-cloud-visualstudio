using System.IO;

namespace GoogleCloudExtension.GcsFileBrowser
{
    internal class GcsUpload
    {
        public string SourcePath { get; }

        public string DestGcsPath => $"gs://{Bucket}/{Name}";

        public string Bucket { get; }

        public string Name { get; }

        public GcsUpload(
            string sourcePath,
            string bucket,
            string namePrefix)
        {
            SourcePath = sourcePath;
            Bucket = bucket;
            Name = namePrefix + Path.GetFileName(sourcePath);
        }
    }
}
