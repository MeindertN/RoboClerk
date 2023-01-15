// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
//using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RoboClerk.Redmine
{
    public class RedmineProject
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("identifier")]
        public string Identifier { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("is_public")]
        public bool IsPublic { get; set; }

        [JsonPropertyName("inherit_members")]
        public bool InheritMembers { get; set; }

        [JsonPropertyName("created_on")]
        public DateTime? CreatedOn { get; set; }

        [JsonPropertyName("updated_on")]
        public DateTime? UpdatedOn { get; set; }

        [JsonPropertyName("parent")]
        public Parent Parent { get; set; }
    }

    public class DefaultStatus
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class RedmineTracker
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("default_status")]
        public DefaultStatus DefaultStatus { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    public class Status
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class Priority
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class Author
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class AssignedTo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class FixedVersion
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class CustomField
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("multiple")]
        public bool Multiple { get; set; }

        [JsonPropertyName("value")]
        public object Value { get; set; }
    }

    public class Relation
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("issue_id")]
        public int IssueId { get; set; }

        [JsonPropertyName("issue_to_id")]
        public int IssueToId { get; set; }

        [JsonPropertyName("relation_type")]
        public string RelationType { get; set; }

        [JsonPropertyName("delay")]
        public object Delay { get; set; }
    }

    public class Parent
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class RedmineIssue
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("project")]
        public RedmineProject Project { get; set; }

        [JsonPropertyName("tracker")]
        public RedmineTracker Tracker { get; set; }

        [JsonPropertyName("status")]
        public Status Status { get; set; }

        [JsonPropertyName("priority")]
        public Priority Priority { get; set; }

        [JsonPropertyName("author")]
        public Author Author { get; set; }

        [JsonPropertyName("assigned_to")]
        public AssignedTo AssignedTo { get; set; }

        [JsonPropertyName("fixed_version")]
        public FixedVersion FixedVersion { get; set; }

        [JsonPropertyName("subject")]
        public string Subject { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("start_date")]
        public string StartDate { get; set; }

        [JsonPropertyName("due_date")]
        public string DueDate { get; set; }

        [JsonPropertyName("done_ratio")]
        public int DoneRatio { get; set; }

        [JsonPropertyName("is_private")]
        public bool IsPrivate { get; set; }

        [JsonPropertyName("estimated_hours")]
        public object EstimatedHours { get; set; }

        [JsonPropertyName("custom_fields")]
        public List<CustomField> CustomFields { get; set; }

        [JsonPropertyName("created_on")]
        public DateTime? CreatedOn { get; set; }

        [JsonPropertyName("updated_on")]
        public DateTime? UpdatedOn { get; set; }

        [JsonPropertyName("closed_on")]
        public DateTime? ClosedOn { get; set; }

        [JsonPropertyName("relations")]
        public List<Relation> Relations { get; set; }

        [JsonPropertyName("parent")]
        public Parent Parent { get; set; }
    }

    public class RedmineIssues
    {
        [JsonPropertyName("issues")]
        public List<RedmineIssue> Issues { get; set; }

        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }
    }

    public class RedmineProjects
    {
        [JsonPropertyName("projects")]
        public List<RedmineProject> Projects { get; set; }

        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }
    }

    public class RedmineTrackers
    {
        [JsonPropertyName("trackers")]
        public List<RedmineTracker> Trackers { get; set; }
    }
}

