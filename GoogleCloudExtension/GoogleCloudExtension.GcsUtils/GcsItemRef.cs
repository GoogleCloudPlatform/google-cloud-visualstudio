using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GcsUtils
{
    public class GcsItemRef
    {
        public string Bucket { get; }

        public string Name { get; }

        public bool IsDirectory => String.IsNullOrEmpty(Name) || Name.Last() == '/';

        public bool IsFile => !IsDirectory;

        public GcsItemRef(string bucket, string path)
        {
            Bucket = bucket;
            Name = path;
        }
    }
}
