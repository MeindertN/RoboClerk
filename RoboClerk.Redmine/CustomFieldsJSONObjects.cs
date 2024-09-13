using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RoboClerk.Redmine
{
    public class Tracker
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class PossibleValue
    {
        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("label")]
        public string Label { get; set; }
    }

    public class CustomRedmineField
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("customized_type")]
        public string CustomizedType { get; set; }

        [JsonPropertyName("field_format")]
        public string FieldFormat { get; set; }

        [JsonPropertyName("regexp")]
        public string Regexp { get; set; }

        [JsonPropertyName("min_length")]
        public int? MinLength { get; set; }

        [JsonPropertyName("max_length")]
        public int? MaxLength { get; set; }

        [JsonPropertyName("is_required")]
        public bool IsRequired { get; set; }

        [JsonPropertyName("is_filter")]
        public bool IsFilter { get; set; }

        [JsonPropertyName("searchable")]
        public bool Searchable { get; set; }

        [JsonPropertyName("multiple")]
        public bool Multiple { get; set; }

        [JsonPropertyName("default_value")]
        public string DefaultValue { get; set; }

        [JsonPropertyName("visible")]
        public bool Visible { get; set; }

        [JsonPropertyName("possible_values")]
        public List<PossibleValue> PossibleValues { get; set; }

        [JsonPropertyName("trackers")]
        public List<Tracker> Trackers { get; set; }

        [JsonPropertyName("roles")]
        public List<object> Roles { get; set; } // Assuming roles are not detailed in the provided JSON
    }

    public class CustomFieldList
    {
        [JsonPropertyName("custom_fields")]
        public List<CustomRedmineField> CustomFields { get; set; }
    }
}