using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
