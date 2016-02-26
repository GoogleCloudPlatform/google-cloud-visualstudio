using Newtonsoft.Json;

namespace GoogleCloudExtension.DataSources.Models
{
    public class ErrorEntry
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}