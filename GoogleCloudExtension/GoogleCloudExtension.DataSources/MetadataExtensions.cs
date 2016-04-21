using Google.Apis.Compute.v1.Data;
using System.Linq;

namespace GoogleCloudExtension.DataSources
{
    public static class MetadataExtensions
    {
        public static string GetProperty(this Metadata metadata, string key) => 
            metadata.Items?.FirstOrDefault(x => x.Key == key)?.Value;
    }
}
