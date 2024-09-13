using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RoboClerk.Redmine
{
    public class Project
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class Version
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("project")]
        public Project Project { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("due_date")]
        public string DueDate { get; set; }

        [JsonPropertyName("sharing")]
        public string Sharing { get; set; }

        [JsonPropertyName("wiki_page_title")]
        public string WikiPageTitle { get; set; }

        [JsonPropertyName("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonPropertyName("updated_on")]
        public DateTime UpdatedOn { get; set; }
    }

    public class VersionList
    {
        [JsonPropertyName("versions")]
        public List<Version> Versions { get; set; }

        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }
    }
}