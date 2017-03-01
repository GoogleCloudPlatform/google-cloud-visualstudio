using Newtonsoft.Json;

namespace GoogleCloudExtension.GCloud.Models
{
    internal class CloudSdkVersions
    {
        [JsonProperty("Google Cloud SDK")]
        public string SdkVersion { get; set; }
    }
}
