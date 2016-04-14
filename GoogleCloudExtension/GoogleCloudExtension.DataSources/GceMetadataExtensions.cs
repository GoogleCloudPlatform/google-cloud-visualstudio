using GoogleCloudExtension.DataSources.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    public static class GceMetadataExtensions
    {
        public static string GetProperty(this Metadata metadata, string key) => metadata.Items.FirstOrDefault(x => x.Key == key)?.Value;
    }
}
