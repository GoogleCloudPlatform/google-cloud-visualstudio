using Newtonsoft.Json;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    public class GceInstance
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("zone")]
        public string Zone { get; set; }

        [JsonProperty("machineType")]
        public string MachineType { get; set; }

        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }

        public string ZoneName { get; set; }
    }
}