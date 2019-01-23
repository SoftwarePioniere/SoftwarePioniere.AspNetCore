using Newtonsoft.Json;

namespace SoftwarePioniere.AspNetCore.SampleApp
{
    public class ApiInfo
    {
        [JsonProperty("title")]
        public string Title1 { get; set; }

        [JsonProperty("version")]
        public string Version1 { get; set; }
    }

    public class ClaimInfo
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
