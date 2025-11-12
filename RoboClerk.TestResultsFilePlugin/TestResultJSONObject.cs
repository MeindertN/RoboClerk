using System;
using System.Text.Json.Serialization;

namespace RoboClerk.TestResultsFilePlugin
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Skip)]
    public class TestResultJSONObject
    {
        [JsonPropertyName("id")]
        [JsonRequired] 
        public string ID { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        [JsonRequired]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TestType Type { get; set; }

        [JsonPropertyName("status")]
        [JsonRequired]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TestResultStatus Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("executionTime")]
        public DateTime? ExecutionTime { get; set; }

        [JsonPropertyName("project")]
        public string? Project { get; set; }
    }
}
