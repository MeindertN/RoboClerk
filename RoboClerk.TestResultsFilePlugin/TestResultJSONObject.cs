using System;
using System.Text.Json.Serialization;

namespace RoboClerk.TestResultsFilePlugin
{
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
        public TestResultType Type { get; set; }

        [JsonPropertyName("status")]
        [JsonRequired]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TestResultStatus Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("executionTime")]
        public DateTime? ExecutionTime { get; set; }
    }

}
