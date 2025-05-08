using NUnit.Framework;
using NUnit.Framework.Legacy;
using RoboClerk.Redmine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using NSubstitute;
using RoboClerk.Configuration;
using System.IO.Abstractions;
using Tomlyn.Model;

namespace RoboClerk.Redmine.Tests
{
    [TestFixture]
    [Description("These tests test the Redmine plugin JSON object deserialization and RedmineSLMSPlugin functionality")]
    public class TestRedminePlugin
    {
        #region JSON Object Tests

        private string ConvertTomlTableToString(TomlTable table)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var kvp in table)
            {
                if (kvp.Value is TomlTable nestedTable)
                {
                    sb.AppendLine($"[{kvp.Key}]");
                    foreach (var nestedKvp in nestedTable)
                    {
                        if (nestedKvp.Value is bool boolValue)
                        {
                            sb.AppendLine($"{nestedKvp.Key} = {boolValue.ToString().ToLower()}");
                        }
                        else
                        {
                            sb.AppendLine($"{nestedKvp.Key} = \"{nestedKvp.Value}\"");
                        }
                    }
                    sb.AppendLine();
                }
                else if (kvp.Value is bool boolValue)
                {
                    sb.AppendLine($"{kvp.Key} = {boolValue.ToString().ToLower()}");
                }
                else
                {
                    sb.AppendLine($"{kvp.Key} = \"{kvp.Value}\"");
                }
            }
            return sb.ToString();
        }

        [UnitTestAttribute(
        Identifier = "A1B2C3D4-E5F6-7890-ABCD-EF1234567890",
        Purpose = "CustomRedmineField is deserialized correctly",
        PostCondition = "All properties are correctly mapped from JSON")]
        [Test]
        public void TestCustomRedmineFieldDeserialization()
        {
            string json = @"{
                ""id"": 1,
                ""name"": ""Test Field"",
                ""customized_type"": ""issue"",
                ""field_format"": ""string"",
                ""regexp"": ""^[A-Z]+$"",
                ""min_length"": 5,
                ""max_length"": 20,
                ""is_required"": true,
                ""is_filter"": false,
                ""searchable"": true,
                ""multiple"": false,
                ""default_value"": ""DEFAULT"",
                ""visible"": true,
                ""possible_values"": [
                    { ""value"": ""VAL1"", ""label"": ""Value 1"" },
                    { ""value"": ""VAL2"", ""label"": ""Value 2"" }
                ],
                ""trackers"": [
                    { ""id"": 1, ""name"": ""Bug"" },
                    { ""id"": 2, ""name"": ""Feature"" }
                ],
                ""roles"": []
            }";

            var field = JsonSerializer.Deserialize<CustomRedmineField>(json);

            ClassicAssert.NotNull(field);
            ClassicAssert.AreEqual(1, field.Id);
            ClassicAssert.AreEqual("Test Field", field.Name);
            ClassicAssert.AreEqual("issue", field.CustomizedType);
            ClassicAssert.AreEqual("string", field.FieldFormat);
            ClassicAssert.AreEqual("^[A-Z]+$", field.Regexp);
            ClassicAssert.AreEqual(5, field.MinLength);
            ClassicAssert.AreEqual(20, field.MaxLength);
            ClassicAssert.IsTrue(field.IsRequired);
            ClassicAssert.IsFalse(field.IsFilter);
            ClassicAssert.IsTrue(field.Searchable);
            ClassicAssert.IsFalse(field.Multiple);
            ClassicAssert.AreEqual("DEFAULT", field.DefaultValue);
            ClassicAssert.IsTrue(field.Visible);
            ClassicAssert.AreEqual(2, field.PossibleValues.Count);
            ClassicAssert.AreEqual("VAL1", field.PossibleValues[0].Value);
            ClassicAssert.AreEqual("Value 1", field.PossibleValues[0].Label);
            ClassicAssert.AreEqual(2, field.Trackers.Count);
            ClassicAssert.AreEqual("Bug", field.Trackers[0].Name);
            ClassicAssert.NotNull(field.Roles);
        }

        [UnitTestAttribute(
        Identifier = "B2C3D4E5-F6A7-8901-BCDE-F23456789012",
        Purpose = "CustomFieldList is deserialized correctly",
        PostCondition = "All custom fields are properly parsed from JSON")]
        [Test]
        public void TestCustomFieldListDeserialization()
        {
            string json = @"{
                ""custom_fields"": [
                    {
                        ""id"": 1,
                        ""name"": ""Field 1"",
                        ""customized_type"": ""issue"",
                        ""field_format"": ""string"",
                        ""is_required"": true,
                        ""is_filter"": false,
                        ""searchable"": true,
                        ""multiple"": false,
                        ""visible"": true,
                        ""possible_values"": [],
                        ""trackers"": [],
                        ""roles"": []
                    },
                    {
                        ""id"": 2,
                        ""name"": ""Field 2"",
                        ""customized_type"": ""issue"",
                        ""field_format"": ""int"",
                        ""is_required"": false,
                        ""is_filter"": true,
                        ""searchable"": false,
                        ""multiple"": true,
                        ""visible"": true,
                        ""possible_values"": [],
                        ""trackers"": [],
                        ""roles"": []
                    }
                ]
            }";

            var list = JsonSerializer.Deserialize<CustomFieldList>(json);

            ClassicAssert.NotNull(list);
            ClassicAssert.NotNull(list.CustomFields);
            ClassicAssert.AreEqual(2, list.CustomFields.Count);
            ClassicAssert.AreEqual("Field 1", list.CustomFields[0].Name);
            ClassicAssert.AreEqual("Field 2", list.CustomFields[1].Name);
            ClassicAssert.IsTrue(list.CustomFields[0].IsRequired);
            ClassicAssert.IsFalse(list.CustomFields[1].IsRequired);
            ClassicAssert.IsTrue(list.CustomFields[1].Multiple);
        }

        [UnitTestAttribute(
        Identifier = "C3D4E5F6-A789-0123-CDEF-34567890123A",
        Purpose = "Version object is deserialized correctly",
        PostCondition = "All version properties including dates are correctly parsed")]
        [Test]
        public void TestVersionDeserialization()
        {
            string json = @"{
                ""id"": 1,
                ""project"": {
                    ""id"": 2,
                    ""name"": ""Test Project""
                },
                ""name"": ""Version 1.0"",
                ""description"": ""First release"",
                ""status"": ""open"",
                ""due_date"": ""2024-01-01"",
                ""sharing"": ""none"",
                ""wiki_page_title"": ""Version_1.0"",
                ""created_on"": ""2023-01-01T10:00:00Z"",
                ""updated_on"": ""2023-06-01T15:30:00Z""
            }";

            var version = JsonSerializer.Deserialize<Version>(json);

            ClassicAssert.NotNull(version);
            ClassicAssert.AreEqual(1, version.Id);
            ClassicAssert.NotNull(version.Project);
            ClassicAssert.AreEqual(2, version.Project.Id);
            ClassicAssert.AreEqual("Test Project", version.Project.Name);
            ClassicAssert.AreEqual("Version 1.0", version.Name);
            ClassicAssert.AreEqual("First release", version.Description);
            ClassicAssert.AreEqual("open", version.Status);
            ClassicAssert.AreEqual("2024-01-01", version.DueDate);
            ClassicAssert.AreEqual("none", version.Sharing);
            ClassicAssert.AreEqual("Version_1.0", version.WikiPageTitle);
            ClassicAssert.AreEqual(new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc), version.CreatedOn);
            ClassicAssert.AreEqual(new DateTime(2023, 6, 1, 15, 30, 0, DateTimeKind.Utc), version.UpdatedOn);
        }

        [UnitTestAttribute(
        Identifier = "D4E5F6A7-B890-1234-DEF0-4567890123AB",
        Purpose = "VersionList with multiple versions is deserialized correctly",
        PostCondition = "All versions in the list are properly parsed")]
        [Test]
        public void TestVersionListDeserialization()
        {
            string json = @"{
                ""versions"": [
                    {
                        ""id"": 1,
                        ""project"": { ""id"": 2, ""name"": ""Test Project"" },
                        ""name"": ""Version 1.0"",
                        ""description"": ""First release"",
                        ""status"": ""open"",
                        ""due_date"": ""2024-01-01"",
                        ""sharing"": ""none"",
                        ""wiki_page_title"": ""Version_1.0"",
                        ""created_on"": ""2023-01-01T10:00:00Z"",
                        ""updated_on"": ""2023-06-01T15:30:00Z""
                    },
                    {
                        ""id"": 2,
                        ""project"": { ""id"": 2, ""name"": ""Test Project"" },
                        ""name"": ""Version 2.0"",
                        ""description"": ""Second release"",
                        ""status"": ""locked"",
                        ""due_date"": ""2024-06-01"",
                        ""sharing"": ""none"",
                        ""wiki_page_title"": ""Version_2.0"",
                        ""created_on"": ""2023-06-01T10:00:00Z"",
                        ""updated_on"": ""2023-12-01T15:30:00Z""
                    }
                ],
                ""total_count"": 2
            }";

            var versionList = JsonSerializer.Deserialize<VersionList>(json);

            ClassicAssert.NotNull(versionList);
            ClassicAssert.NotNull(versionList.Versions);
            ClassicAssert.AreEqual(2, versionList.Versions.Count);
            ClassicAssert.AreEqual(2, versionList.TotalCount);
            ClassicAssert.AreEqual("Version 1.0", versionList.Versions[0].Name);
            ClassicAssert.AreEqual("Version 2.0", versionList.Versions[1].Name);
        }

        [UnitTestAttribute(
        Identifier = "E5F6A7B8-C901-2345-EF01-567890123ABC",
        Purpose = "RedmineIssue with all fields is deserialized correctly",
        PostCondition = "All issue properties including nested objects are properly parsed")]
        [Test]
        public void TestRedmineIssueDeserialization()
        {
            string json = @"{
                ""id"": 1,
                ""project"": {
                    ""id"": 1,
                    ""name"": ""Test Project"",
                    ""identifier"": ""test-project"",
                    ""description"": ""A test project"",
                    ""status"": 1,
                    ""is_public"": true,
                    ""inherit_members"": false,
                    ""created_on"": ""2023-01-01T10:00:00Z"",
                    ""updated_on"": ""2023-06-01T15:30:00Z"",
                    ""parent"": null
                },
                ""tracker"": {
                    ""id"": 1,
                    ""name"": ""Bug"",
                    ""default_status"": {
                        ""id"": 1,
                        ""name"": ""New""
                    },
                    ""description"": ""Bug tracker""
                },
                ""status"": {
                    ""id"": 1,
                    ""name"": ""New""
                },
                ""priority"": {
                    ""id"": 2,
                    ""name"": ""Normal""
                },
                ""author"": {
                    ""id"": 1,
                    ""name"": ""John Doe""
                },
                ""assigned_to"": {
                    ""id"": 2,
                    ""name"": ""Jane Smith""
                },
                ""fixed_version"": {
                    ""id"": 1,
                    ""name"": ""Version 1.0""
                },
                ""subject"": ""Test Issue"",
                ""description"": ""This is a test issue"",
                ""start_date"": ""2023-01-01"",
                ""due_date"": ""2023-02-01"",
                ""done_ratio"": 50,
                ""is_private"": false,
                ""estimated_hours"": 10.5,
                ""custom_fields"": [
                    {
                        ""id"": 1,
                        ""name"": ""Severity"",
                        ""multiple"": false,
                        ""value"": ""High""
                    }
                ],
                ""created_on"": ""2023-01-01T10:00:00Z"",
                ""updated_on"": ""2023-06-01T15:30:00Z"",
                ""closed_on"": ""2023-07-01T12:00:00Z"",
                ""relations"": [
                    {
                        ""id"": 1,
                        ""issue_id"": 1,
                        ""issue_to_id"": 2,
                        ""relation_type"": ""relates"",
                        ""delay"": null
                    }
                ],
                ""parent"": {
                    ""id"": 10,
                    ""name"": ""Parent Issue""
                }
            }";

            var issue = JsonSerializer.Deserialize<RedmineIssue>(json);

            ClassicAssert.NotNull(issue);
            ClassicAssert.AreEqual(1, issue.Id);
            ClassicAssert.NotNull(issue.Project);
            ClassicAssert.AreEqual("Test Project", issue.Project.Name);
            ClassicAssert.NotNull(issue.Tracker);
            ClassicAssert.AreEqual("Bug", issue.Tracker.Name);
            ClassicAssert.NotNull(issue.Status);
            ClassicAssert.AreEqual("New", issue.Status.Name);
            ClassicAssert.NotNull(issue.Priority);
            ClassicAssert.AreEqual("Normal", issue.Priority.Name);
            ClassicAssert.NotNull(issue.Author);
            ClassicAssert.AreEqual("John Doe", issue.Author.Name);
            ClassicAssert.NotNull(issue.AssignedTo);
            ClassicAssert.AreEqual("Jane Smith", issue.AssignedTo.Name);
            ClassicAssert.NotNull(issue.FixedVersion);
            ClassicAssert.AreEqual("Version 1.0", issue.FixedVersion.Name);
            ClassicAssert.AreEqual("Test Issue", issue.Subject);
            ClassicAssert.AreEqual("This is a test issue", issue.Description);
            ClassicAssert.AreEqual("2023-01-01", issue.StartDate);
            ClassicAssert.AreEqual("2023-02-01", issue.DueDate);
            ClassicAssert.AreEqual(50, issue.DoneRatio);
            ClassicAssert.IsFalse(issue.IsPrivate);
            ClassicAssert.NotNull(issue.EstimatedHours);
            ClassicAssert.AreEqual(10.5, ((JsonElement)issue.EstimatedHours).GetDouble());
            ClassicAssert.NotNull(issue.CustomFields);
            ClassicAssert.AreEqual(1, issue.CustomFields.Count);
            ClassicAssert.AreEqual("Severity", issue.CustomFields[0].Name);
            ClassicAssert.AreEqual("High", ((JsonElement)issue.CustomFields[0].Value).GetString());
            ClassicAssert.NotNull(issue.Relations);
            ClassicAssert.AreEqual(1, issue.Relations.Count);
            ClassicAssert.AreEqual(1, issue.Relations[0].IssueId);
            ClassicAssert.AreEqual(2, issue.Relations[0].IssueToId);
            ClassicAssert.AreEqual("relates", issue.Relations[0].RelationType);
            ClassicAssert.NotNull(issue.Parent);
            ClassicAssert.AreEqual(10, issue.Parent.Id);
            ClassicAssert.AreEqual("Parent Issue", issue.Parent.Name);
        }

        [UnitTestAttribute(
        Identifier = "F6A7B8C9-D012-3456-F012-67890123ABCD",
        Purpose = "RedmineIssues list with multiple issues is deserialized correctly",
        PostCondition = "All issues and pagination information are properly parsed")]
        [Test]
        public void TestRedmineIssuesDeserialization()
        {
            string json = @"{
                ""issues"": [
                    {
                        ""id"": 1,
                        ""subject"": ""Issue 1"",
                        ""project"": { ""id"": 1, ""name"": ""Project 1"" },
                        ""tracker"": { ""id"": 1, ""name"": ""Bug"" },
                        ""status"": { ""id"": 1, ""name"": ""New"" },
                        ""priority"": { ""id"": 2, ""name"": ""Normal"" },
                        ""author"": { ""id"": 1, ""name"": ""Author 1"" },
                        ""created_on"": ""2023-01-01T10:00:00Z"",
                        ""updated_on"": ""2023-06-01T15:30:00Z"",
                        ""custom_fields"": [],
                        ""relations"": []
                    },
                    {
                        ""id"": 2,
                        ""subject"": ""Issue 2"",
                        ""project"": { ""id"": 1, ""name"": ""Project 1"" },
                        ""tracker"": { ""id"": 2, ""name"": ""Feature"" },
                        ""status"": { ""id"": 2, ""name"": ""In Progress"" },
                        ""priority"": { ""id"": 3, ""name"": ""High"" },
                        ""author"": { ""id"": 2, ""name"": ""Author 2"" },
                        ""created_on"": ""2023-01-15T11:00:00Z"",
                        ""updated_on"": ""2023-06-15T16:30:00Z"",
                        ""custom_fields"": [],
                        ""relations"": []
                    }
                ],
                ""total_count"": 2,
                ""offset"": 0,
                ""limit"": 25
            }";

            var issues = JsonSerializer.Deserialize<RedmineIssues>(json);

            ClassicAssert.NotNull(issues);
            ClassicAssert.NotNull(issues.Issues);
            ClassicAssert.AreEqual(2, issues.Issues.Count);
            ClassicAssert.AreEqual(2, issues.TotalCount);
            ClassicAssert.AreEqual(0, issues.Offset);
            ClassicAssert.AreEqual(25, issues.Limit);
            ClassicAssert.AreEqual("Issue 1", issues.Issues[0].Subject);
            ClassicAssert.AreEqual("Issue 2", issues.Issues[1].Subject);
            ClassicAssert.AreEqual("Bug", issues.Issues[0].Tracker.Name);
            ClassicAssert.AreEqual("Feature", issues.Issues[1].Tracker.Name);
        }

        [UnitTestAttribute(
        Identifier = "A7B8C9D0-E123-4567-A123-7890123ABCDE",
        Purpose = "RedmineProject is deserialized correctly",
        PostCondition = "All project properties are properly parsed")]
        [Test]
        public void TestRedmineProjectDeserialization()
        {
            string json = @"{
                ""id"": 1,
                ""name"": ""Test Project"",
                ""identifier"": ""test-project"",
                ""description"": ""A test project description"",
                ""status"": 1,
                ""is_public"": true,
                ""inherit_members"": false,
                ""created_on"": ""2023-01-01T10:00:00Z"",
                ""updated_on"": ""2023-06-01T15:30:00Z"",
                ""parent"": {
                    ""id"": 2,
                    ""name"": ""Parent Project""
                }
            }";

            var project = JsonSerializer.Deserialize<RedmineProject>(json);

            ClassicAssert.NotNull(project);
            ClassicAssert.AreEqual(1, project.Id);
            ClassicAssert.AreEqual("Test Project", project.Name);
            ClassicAssert.AreEqual("test-project", project.Identifier);
            ClassicAssert.AreEqual("A test project description", project.Description);
            ClassicAssert.AreEqual(1, project.Status);
            ClassicAssert.IsTrue(project.IsPublic);
            ClassicAssert.IsFalse(project.InheritMembers);
            ClassicAssert.AreEqual(new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc), project.CreatedOn);
            ClassicAssert.AreEqual(new DateTime(2023, 6, 1, 15, 30, 0, DateTimeKind.Utc), project.UpdatedOn);
            ClassicAssert.NotNull(project.Parent);
            ClassicAssert.AreEqual(2, project.Parent.Id);
            ClassicAssert.AreEqual("Parent Project", project.Parent.Name);
        }

        [UnitTestAttribute(
        Identifier = "B8C9D0E1-F234-5678-B234-890123ABCDEF",
        Purpose = "RedmineTracker is deserialized correctly",
        PostCondition = "All tracker properties including default status are properly parsed")]
        [Test]
        public void TestRedmineTrackerDeserialization()
        {
            string json = @"{
                ""id"": 1,
                ""name"": ""Bug"",
                ""default_status"": {
                    ""id"": 1,
                    ""name"": ""New""
                },
                ""description"": ""Bug tracker description""
            }";

            var tracker = JsonSerializer.Deserialize<RedmineTracker>(json);

            ClassicAssert.NotNull(tracker);
            ClassicAssert.AreEqual(1, tracker.Id);
            ClassicAssert.AreEqual("Bug", tracker.Name);
            ClassicAssert.NotNull(tracker.DefaultStatus);
            ClassicAssert.AreEqual(1, tracker.DefaultStatus.Id);
            ClassicAssert.AreEqual("New", tracker.DefaultStatus.Name);
            ClassicAssert.AreEqual("Bug tracker description", tracker.Description);
        }

        [UnitTestAttribute(
        Identifier = "C9D0E1F2-A345-6789-D345-91234ABCDEF0",
        Purpose = "RedmineProjects list with multiple projects is deserialized correctly",
        PostCondition = "All projects and pagination information are properly parsed")]
        [Test]
        public void TestRedmineProjectsDeserialization()
        {
            string json = @"{
                ""projects"": [
                    {
                        ""id"": 1,
                        ""name"": ""Project 1"",
                        ""identifier"": ""project-1"",
                        ""description"": ""First test project"",
                        ""status"": 1,
                        ""is_public"": true,
                        ""inherit_members"": false,
                        ""created_on"": ""2023-01-01T10:00:00Z"",
                        ""updated_on"": ""2023-06-01T15:30:00Z""
                    },
                    {
                        ""id"": 2,
                        ""name"": ""Project 2"",
                        ""identifier"": ""project-2"", 
                        ""description"": ""Second test project"",
                        ""status"": 1,
                        ""is_public"": false,
                        ""inherit_members"": true,
                        ""created_on"": ""2023-02-01T11:00:00Z"",
                        ""updated_on"": ""2023-07-01T16:30:00Z"",
                        ""parent"": {
                            ""id"": 1,
                            ""name"": ""Project 1""
                        }
                    }
                ],
                ""total_count"": 2,
                ""offset"": 0,
                ""limit"": 25
            }";

            var projects = JsonSerializer.Deserialize<RedmineProjects>(json);

            ClassicAssert.NotNull(projects);
            ClassicAssert.NotNull(projects.Projects);
            ClassicAssert.AreEqual(2, projects.Projects.Count);
            ClassicAssert.AreEqual(2, projects.TotalCount);
            ClassicAssert.AreEqual(0, projects.Offset);
            ClassicAssert.AreEqual(25, projects.Limit);

            // Verify first project
            ClassicAssert.AreEqual(1, projects.Projects[0].Id);
            ClassicAssert.AreEqual("Project 1", projects.Projects[0].Name);
            ClassicAssert.AreEqual("project-1", projects.Projects[0].Identifier);
            ClassicAssert.AreEqual("First test project", projects.Projects[0].Description);
            ClassicAssert.IsTrue(projects.Projects[0].IsPublic);
            ClassicAssert.IsFalse(projects.Projects[0].InheritMembers);
            ClassicAssert.IsNull(projects.Projects[0].Parent);

            // Verify second project
            ClassicAssert.AreEqual(2, projects.Projects[1].Id);
            ClassicAssert.AreEqual("Project 2", projects.Projects[1].Name);
            ClassicAssert.AreEqual("project-2", projects.Projects[1].Identifier);
            ClassicAssert.AreEqual("Second test project", projects.Projects[1].Description);
            ClassicAssert.IsFalse(projects.Projects[1].IsPublic);
            ClassicAssert.IsTrue(projects.Projects[1].InheritMembers);
            ClassicAssert.NotNull(projects.Projects[1].Parent);
            ClassicAssert.AreEqual(1, projects.Projects[1].Parent.Id);
            ClassicAssert.AreEqual("Project 1", projects.Projects[1].Parent.Name);
        }

        [UnitTestAttribute(
        Identifier = "D0E1F2A3-B456-789A-E456-01235ABCDEF1",
        Purpose = "RedmineTrackers list with multiple trackers is deserialized correctly",
        PostCondition = "All trackers are properly parsed")]
        [Test]
        public void TestRedmineTrackersDeserialization()
        {
            string json = @"{
                ""trackers"": [
                    {
                        ""id"": 1,
                        ""name"": ""Bug"",
                        ""default_status"": {
                            ""id"": 1,
                            ""name"": ""New""
                        },
                        ""description"": ""Bug tracker""
                    },
                    {
                        ""id"": 2,
                        ""name"": ""Feature"",
                        ""default_status"": {
                            ""id"": 1,
                            ""name"": ""New""
                        },
                        ""description"": ""Feature tracker""
                    },
                    {
                        ""id"": 3,
                        ""name"": ""Support"",
                        ""default_status"": {
                            ""id"": 2,
                            ""name"": ""Open""
                        },
                        ""description"": ""Support request tracker""
                    }
                ]
            }";

            var trackers = JsonSerializer.Deserialize<RedmineTrackers>(json);

            ClassicAssert.NotNull(trackers);
            ClassicAssert.NotNull(trackers.Trackers);
            ClassicAssert.AreEqual(3, trackers.Trackers.Count);

            // Verify first tracker
            ClassicAssert.AreEqual(1, trackers.Trackers[0].Id);
            ClassicAssert.AreEqual("Bug", trackers.Trackers[0].Name);
            ClassicAssert.AreEqual("Bug tracker", trackers.Trackers[0].Description);
            ClassicAssert.NotNull(trackers.Trackers[0].DefaultStatus);
            ClassicAssert.AreEqual(1, trackers.Trackers[0].DefaultStatus.Id);
            ClassicAssert.AreEqual("New", trackers.Trackers[0].DefaultStatus.Name);

            // Verify second tracker
            ClassicAssert.AreEqual(2, trackers.Trackers[1].Id);
            ClassicAssert.AreEqual("Feature", trackers.Trackers[1].Name);
            ClassicAssert.AreEqual("Feature tracker", trackers.Trackers[1].Description);

            // Verify third tracker
            ClassicAssert.AreEqual(3, trackers.Trackers[2].Id);
            ClassicAssert.AreEqual("Support", trackers.Trackers[2].Name);
            ClassicAssert.AreEqual("Support request tracker", trackers.Trackers[2].Description);
            ClassicAssert.AreEqual(2, trackers.Trackers[2].DefaultStatus.Id);
            ClassicAssert.AreEqual("Open", trackers.Trackers[2].DefaultStatus.Name);
        }

        #endregion

        #region RedmineSLMSPlugin Tests

        private IFileSystem fileSystem;
        private IConfiguration configuration;

        [SetUp]
        public void SetUp()
        {
            fileSystem = Substitute.For<IFileSystem>();
            configuration = Substitute.For<IConfiguration>();
        }

        [UnitTestAttribute(
        Identifier = "C9D0E1F2-A345-6789-C345-90123ABCDEF0",
        Purpose = "RedmineSLMSPlugin is created successfully",
        PostCondition = "No exception is thrown and plugin has correct name and description")]
        [Test]
        public void TestRedmineSLMSPluginCreation()
        {
            var plugin = new RedmineSLMSPlugin(fileSystem);

            ClassicAssert.AreEqual("RedmineSLMSPlugin", plugin.Name);
            ClassicAssert.AreEqual("A plugin that can interrogate Redmine via its REST API to retrieve information needed by RoboClerk to create documentation.", plugin.Description);
        }

        [UnitTestAttribute(
        Identifier = "D0E1F2A3-B456-789A-D456-0123ABCDEF01",
        Purpose = "RedmineSLMSPlugin initialization works with valid configuration",
        PostCondition = "Plugin is initialized without throwing exceptions")]
        [Test]
        public void TestRedmineSLMSPluginInitialization()
        {
            var plugin = new RedmineSLMSPlugin(fileSystem);

            // Setup mock configuration file content
            var configTable = new TomlTable();
            configTable["RedmineAPIEndpoint"] = "http://localhost:3001/";
            configTable["RedmineAPIKey"] = "test_api_key";
            configTable["RedmineProject"] = "TestProject";
            configTable["RedmineBaseURL"] = "http://localhost:3001/issues/";
            configTable["ConvertTextile"] = true;

            // Setup truth item configurations
            var sysReqConfig = new TomlTable();
            sysReqConfig["name"] = "SystemRequirement";
            sysReqConfig["filter"] = true;
            configTable["SystemRequirement"] = sysReqConfig;

            var softReqConfig = new TomlTable();
            softReqConfig["name"] = "SoftwareRequirement";
            softReqConfig["filter"] = true;
            configTable["SoftwareRequirement"] = softReqConfig;

            var docReqConfig = new TomlTable();
            docReqConfig["name"] = "Documentation";
            docReqConfig["filter"] = true;
            configTable["DocumentationRequirement"] = docReqConfig;

            var docContentConfig = new TomlTable();
            docContentConfig["name"] = "DocContent";
            docContentConfig["filter"] = false;
            configTable["DocContent"] = docContentConfig;

            var testCaseConfig = new TomlTable();
            testCaseConfig["name"] = "SoftwareSystemTest";
            testCaseConfig["filter"] = false;
            configTable["SoftwareSystemTest"] = testCaseConfig;

            var anomalyConfig = new TomlTable();
            anomalyConfig["name"] = "Bug";
            anomalyConfig["filter"] = false;
            configTable["Anomaly"] = anomalyConfig;

            var riskConfig = new TomlTable();
            riskConfig["name"] = "Risk";
            riskConfig["filter"] = true;
            configTable["Risk"] = riskConfig;

            var soupConfig = new TomlTable();
            soupConfig["name"] = "SOUP";
            soupConfig["filter"] = true;
            configTable["SOUP"] = soupConfig;

            // Mock file system calls
            configuration.PluginConfigDir.Returns("TestPluginDir");
            fileSystem.Path.GetDirectoryName(Arg.Any<string>()).Returns("TestLocation");
            fileSystem.Path.Combine(Arg.Any<string>(), Arg.Any<string>()).Returns("TestLocation/Configuration/RedmineSLMSPlugin.toml");
            fileSystem.File.ReadAllText(Arg.Any<string>()).Returns(ConvertTomlTableToString(configTable));

            // Mock CommandLineOptionOrDefault to return the config values
            configuration.CommandLineOptionOrDefault("RedmineAPIEndpoint", Arg.Any<string>()).Returns("http://localhost:3001/");
            configuration.CommandLineOptionOrDefault("RedmineAPIKey", Arg.Any<string>()).Returns("test_api_key");
            configuration.CommandLineOptionOrDefault("RedmineProject", Arg.Any<string>()).Returns("TestProject");
            configuration.CommandLineOptionOrDefault("RedmineBaseURL", Arg.Any<string>()).Returns("http://localhost:3001/issues/");
            configuration.CommandLineOptionOrDefault("ConvertTextile", Arg.Any<string>()).Returns("TRUE");

            // Assert that initialization doesn't throw
            ClassicAssert.DoesNotThrow(() => plugin.Initialize(configuration));
        }

        [UnitTestAttribute(
        Identifier = "E1F2A3B4-C567-89AB-E567-123ABCDEF012",
        Purpose = "RedmineSLMSPlugin throws when required configuration is missing",
        PostCondition = "Exception is thrown for missing required configuration")]
        [Test]
        public void TestRedmineSLMSPluginMissingConfiguration()
        {
            var plugin = new RedmineSLMSPlugin(fileSystem);

            // Setup incomplete configuration
            var configTable = new TomlTable();
            configTable["RedmineAPIEndpoint"] = "http://localhost:3001/";
            // Missing RedmineAPIKey and other required fields

            // Mock file system calls
            configuration.PluginConfigDir.Returns("TestPluginDir");
            fileSystem.Path.GetDirectoryName(Arg.Any<string>()).Returns("TestLocation");
            fileSystem.Path.Combine(Arg.Any<string>(), Arg.Any<string>()).Returns("TestLocation/Configuration/RedmineSLMSPlugin.toml");
            fileSystem.File.ReadAllText(Arg.Any<string>()).Returns(ConvertTomlTableToString(configTable));

            // Assert that initialization throws
            ClassicAssert.Throws<Exception>(() => plugin.Initialize(configuration));
        }
        #endregion
    }
}