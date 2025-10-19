using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RoboClerk.TestDescriptionFilePlugin
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Skip)]
    public class TestDescriptionJSONObject
    {
        [JsonPropertyName("id")]
        [JsonRequired] 
        public string ID { get; set; }

        [JsonPropertyName("name")]
        [JsonRequired]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        [JsonRequired]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TestType Type { get; set; }

        [JsonPropertyName("description")]
        [JsonRequired]
        public string Description { get; set; }

        [JsonPropertyName("trace")]
        [JsonRequired]
        public List<string> Trace { get; set; } = new();

        [JsonPropertyName("filename")]
        public string? Filename { get; set; }

        [JsonPropertyName("purpose")]
        public string? Purpose { get; set; }

        [JsonPropertyName("acceptance")]
        public string? Acceptance { get; set; }
    }
}
