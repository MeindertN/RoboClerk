using NUnit.Framework;
using NUnit.Framework.Legacy;
using RoboClerk.Redmine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using NSubstitute;
using RoboClerk.Core.Configuration;
using System.IO.Abstractions;
using Tomlyn.Model;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using RoboClerk;
using System.Threading.Tasks;

namespace RoboClerk.Redmine.Tests
{
    [TestFixture]
    [Description("These tests test the Redmine plugin JSON object deserialization and RedmineSLMSPlugin functionality")]
    public class TestRedminePlugin
    {
        private class TestRedmineSLMSPlugin : RedmineSLMSPlugin
        {
            public TestRedmineSLMSPlugin(IFileProviderPlugin fileSystem, IRedmineClient client) 
                : base(fileSystem, client)
            {
            }

            // Expose protected methods for testing
            public new bool ShouldIgnoreIssue(RedmineIssue redmineItem, TruthItemConfig config, out string reason)
            {
                return base.ShouldIgnoreIssue(redmineItem, config, out reason);
            }

            public new SoftwareSystemTestItem CreateTestCase(List<RedmineIssue> issues, RedmineIssue redmineItem)
            {
                return base.CreateTestCase(issues, redmineItem);
            }

            public new AnomalyItem CreateBug(RedmineIssue redmineItem)
            {
                return base.CreateBug(redmineItem);
            }

            public new RequirementItem CreateRequirement(List<RedmineIssue> issues, RedmineIssue redmineItem, RequirementType requirementType)
            {
                return base.CreateRequirement(issues, redmineItem, requirementType);
            }

            public new DocContentItem CreateDocContent(RedmineIssue redmineItem)
            {
                return base.CreateDocContent(redmineItem);
            }

            public new SOUPItem CreateSOUP(RedmineIssue redmineItem)
            {
                return base.CreateSOUP(redmineItem);
            }

            public new RiskItem CreateRisk(List<RedmineIssue> issues, RedmineIssue redmineItem)
            {
                return base.CreateRisk(issues, redmineItem);
            }

            // Expose protected collections for testing
            public new List<RequirementItem> softwareRequirements => base.softwareRequirements;
            public new List<SoftwareSystemTestItem> testCases => base.testCases;
            public new List<EliminatedRequirementItem> eliminatedSoftwareRequirements => base.eliminatedSoftwareRequirements;
            public new List<EliminatedSoftwareSystemTestItem> eliminatedSoftwareSystemTests => base.eliminatedSoftwareSystemTests;
        }

        #region Test Setup Helpers

        private IFileSystem fileSystem;
        private IFileProviderPlugin fileProviderPlugin;
        private IConfiguration configuration;
        private IRedmineClient redmineClient;
        private TestRedmineSLMSPlugin plugin;

        [SetUp]
        public void SetUp()
        {
            fileSystem = Substitute.For<IFileSystem>();
            fileProviderPlugin = new LocalFileSystemPlugin(fileSystem);
            configuration = Substitute.For<IConfiguration>();
            redmineClient = Substitute.For<IRedmineClient>();
            plugin = new TestRedmineSLMSPlugin(fileProviderPlugin, redmineClient);
        }

        private TomlTable CreateBaseConfiguration()
        {
            var configTable = new TomlTable();
            configTable["RedmineAPIEndpoint"] = "http://localhost:3001/";
            configTable["RedmineAPIKey"] = "test_api_key";
            configTable["RedmineProjects"] = new TomlArray() { "TestProject" };
            configTable["RedmineBaseURL"] = "http://localhost:3001/issues/";
            configTable["ConvertTextile"] = false;
            return configTable;
        }

        private void SetupTruthItemConfigurations(TomlTable configTable, Dictionary<string, string> trackerNames)
        {
            foreach (var kvp in trackerNames)
            {
                configTable[kvp.Key] = new TomlTable { ["name"] = kvp.Value, ["filter"] = false };
            }
        }

        private void SetupMockFileSystem()
        {
            configuration.PluginConfigDir.Returns("TestPluginDir");
            fileSystem.Path.GetDirectoryName(Arg.Any<string>()).Returns("TestLocation");
            fileSystem.Path.Combine(Arg.Any<string[]>()).Returns("TestLocation/Configuration/RedmineSLMSPlugin.toml");
            fileSystem.Path.Combine(Arg.Any<string>(),Arg.Any<string>()).Returns("TestLocation/Configuration/RedmineSLMSPlugin.toml");
            fileProviderPlugin.FileExists("TestLocation/Configuration/RedmineSLMSPlugin.toml").Returns(true);
        }

        private void SetupMockConfiguration()
        {
            configuration.CommandLineOptionOrDefault("RedmineAPIEndpoint", Arg.Any<string>()).Returns("http://localhost:3001/");
            configuration.CommandLineOptionOrDefault("RedmineAPIKey", Arg.Any<string>()).Returns("test_api_key");
            configuration.CommandLineOptionOrDefault("RedmineBaseURL", Arg.Any<string>()).Returns("http://localhost:3001/issues/");
            configuration.CommandLineOptionOrDefault("ConvertTextile", Arg.Any<string>()).Returns("FALSE");
        }

        private void SetupMockRedmineResponses()
        {
            // Mock projects response
            var projectsResponse = new RedmineProjects
            {
                Projects = new List<RedmineProject>
                {
                    new RedmineProject { Id = 1, Name = "TestProject" }
                }
            };
            redmineClient.GetAsync<RedmineProjects>(Arg.Any<RestRequest>())
                .Returns(projectsResponse);

            // Mock versions response
            var versionsResponse = new VersionList
            {
                Versions = new List<Version>
                {
                    new Version { Id = 1, Name = "1.0" }
                }
            };
            redmineClient.GetAsync<VersionList>(Arg.Any<RestRequest>())
                .Returns(versionsResponse);

            // Mock trackers response
            var trackersResponse = new RedmineTrackers
            {
                Trackers = new List<RedmineTracker>
                {
                    new RedmineTracker { Id = 1, Name = "SystemRequirement" },
                    new RedmineTracker { Id = 2, Name = "SoftwareRequirement" },
                    new RedmineTracker { Id = 3, Name = "Documentation" },
                    new RedmineTracker { Id = 4, Name = "SoftwareSystemTest" },
                    new RedmineTracker { Id = 5, Name = "Bug" },
                    new RedmineTracker { Id = 6, Name = "Risk" },
                    new RedmineTracker { Id = 7, Name = "SOUP" },
                    new RedmineTracker { Id = 8, Name = "DocContent" }
                }
            };
            redmineClient.GetAsync<RedmineTrackers>(Arg.Any<RestRequest>())
                .Returns(trackersResponse);

            // Mock issues response
            var issuesResponse = new RedmineIssues
            {
                Issues = new List<RedmineIssue>(),
                TotalCount = 0,
                Offset = 0,
                Limit = 100
            };
            redmineClient.GetAsync<RedmineIssues>(Arg.Any<RestRequest>())
                .Returns(issuesResponse);
        }

        private void InitializePluginWithConfiguration(TomlTable configTable)
        {
            fileSystem.File.ReadAllText(Arg.Any<string>()).Returns(ConvertTomlTableToString(configTable));
            plugin.InitializePlugin(configuration);
        }

        #endregion

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
                        if (nestedKvp.Value is TomlArray arrayValue)
                        {
                            sb.Append($"{nestedKvp.Key} = [");
                            for (int i = 0; i < arrayValue.Count; i++)
                            {
                                if (i > 0) sb.Append(", ");
                                sb.Append($"\"{arrayValue[i]}\"");
                            }
                            sb.AppendLine("]");
                        }
                        else if (nestedKvp.Value is bool boolValue)
                        {
                            sb.AppendLine($"{nestedKvp.Key} = {boolValue.ToString().ToLower()}");
                        }
                        else if (nestedKvp.Value is string stringValue)
                        {
                            sb.AppendLine($"{nestedKvp.Key} = \"{stringValue}\"");
                        }
                        else
                        {
                            sb.AppendLine($"{nestedKvp.Key} = \"{nestedKvp.Value}\"");
                        }
                    }
                    sb.AppendLine();
                }
                else if (kvp.Value is TomlArray arrayValue)
                {
                    sb.Append($"{kvp.Key} = [");
                    for (int i = 0; i < arrayValue.Count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append($"\"{arrayValue[i]}\"");
                    }
                    sb.AppendLine("]");
                }
                else if (kvp.Value is bool boolValue)
                {
                    sb.AppendLine($"{kvp.Key} = {boolValue.ToString().ToLower()}");
                }
                else if (kvp.Value is string stringValue)
                {
                    sb.AppendLine($"{kvp.Key} = \"{stringValue}\"");
                }
                else
                {
                    sb.AppendLine($"{kvp.Key} = \"{kvp.Value}\"");
                }
            }
            return sb.ToString();
        }

        [UnitTestAttribute(
        Identifier = "A1B2C3D4-E5F6-7890-ABED-EF1234567890",
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
        Identifier = "L9C3D4E5-F6A7-8901-BCDE-F23456789012",
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
        Identifier = "3D11F5AD-5799-4630-95C2-176E336179CB",
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
        Identifier = "31B6DC78-5B50-4AB6-9AEF-A070C7D21C29",
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
        Identifier = "047ABA42-1D5B-4A03-94F8-91855900F7F4",
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
        Identifier = "C12BB42F-2340-4B8B-A3E5-A1C6A1AE61DF",
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
        Identifier = "21D61511-45FF-4ABA-8D23-5D59E2F182D8",
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
        Identifier = "9CC3AD74-0056-4CF7-A20E-C150D80F9FAA",
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
        Identifier = "7C0E3033-2A7A-4235-BB7D-1860ADAEA1D7",
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
        Identifier = "6FE12BB3-58C2-41FE-BFFB-0105CC5299F6",
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

        [UnitTestAttribute(
        Identifier = "E7F89012-3456-7890-ABCD-EF0123456789",
        Purpose = "ShouldIgnoreIssue correctly filters issues based on configuration",
        PostCondition = "Issues are correctly filtered based on status and custom fields")]
        [Test]
        public void TestShouldIgnoreIssue()
        {
            // Arrange
            var redmineIssue = new RedmineIssue
            {
                Id = 1,
                Subject = "Test Issue",
                Status = new Status { Name = "Closed" },
                Tracker = new RedmineTracker { Name = "System Requirement" },
                CustomFields = new List<CustomField>
                {
                    new CustomField { Id = 1, Name = "Priority", Value = System.Text.Json.JsonSerializer.SerializeToElement("Low") }
                }
            };

            var config = new TruthItemConfig("System Requirement", true);

            // Setup configuration with filters
            var configTable = new TomlTable();

            // Add required Redmine configuration
            configTable["RedmineAPIEndpoint"] = "http://localhost:3001/";
            configTable["RedmineAPIKey"] = "test_api_key";
            configTable["RedmineProjects"] = new TomlArray() { "TestProject" };
            configTable["RedmineBaseURL"] = "http://localhost:3001/issues/";
            configTable["ConvertTextile"] = true;

            // Add inclusion filter for Status
            var includedFields = new TomlTable();
            var statusValues = new TomlArray();
            statusValues.Add("New");
            statusValues.Add("In Progress");
            includedFields["Status"] = statusValues;
            configTable["IncludedItemFilter"] = includedFields;

            // Add exclusion filter for Priority
            var excludedFields = new TomlTable();
            var priorityValues = new TomlArray();
            priorityValues.Add("Low");
            excludedFields["Priority"] = priorityValues;
            configTable["ExcludedItemFilter"] = excludedFields;

            // Add empty ignore list
            var ignoreList = new TomlArray();
            configTable["Ignore"] = ignoreList;

            // Add all required truth item configurations
            var sysReqConfig = new TomlTable();
            sysReqConfig["name"] = "System Requirement";
            sysReqConfig["filter"] = true;
            configTable["SystemRequirement"] = sysReqConfig;

            var softReqConfig = new TomlTable();
            softReqConfig["name"] = "Software Requirement";
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

            // Mock file system and configuration calls
            SetupMockFileSystem();
            fileSystem.File.ReadAllText(Arg.Any<string>()).Returns(ConvertTomlTableToString(configTable));

            string val = ConvertTomlTableToString(configTable);

            fileProviderPlugin = new LocalFileSystemPlugin(fileSystem);
            var plugin = new TestRedmineSLMSPlugin(fileProviderPlugin, redmineClient);
            plugin.InitializePlugin(configuration);

            // Act
            string reason;
            var shouldIgnore = plugin.ShouldIgnoreIssue(redmineIssue, config, out reason);

            // Assert
            ClassicAssert.IsTrue(shouldIgnore);
            ClassicAssert.IsTrue(reason.Contains("Closed") || reason.Contains("Low"));
        }

        #endregion

        #region RedmineSLMSPlugin Tests

        [UnitTestAttribute(
        Identifier = "C9D0E1F2-A345-6789-C345-901W3ABCDEF0",
        Purpose = "RedmineSLMSPlugin is created successfully",
        PostCondition = "No exception is thrown and plugin has correct name and description")]
        [Test]
        public void TestRedmineSLMSPluginCreation()
        {
            ClassicAssert.AreEqual("RedmineSLMSPlugin", plugin.Name);
            ClassicAssert.AreEqual("A plugin that can interrogate Redmine via its REST API to retrieve information needed by RoboClerk to create documentation.", plugin.Description);
        }

        [UnitTestAttribute(
        Identifier = "D0E1F2F3-B456-789A-D456-0123ABCDEF01",
        Purpose = "RedmineSLMSPlugin initialization works with valid configuration",
        PostCondition = "Plugin is initialized without throwing exceptions")]
        [Test]
        public void TestRedmineSLMSPluginInitialization()
        {
            // Arrange
            var configTable = CreateBaseConfiguration();
            var trackerNames = new Dictionary<string, string>
            {
                ["SystemRequirement"] = "SystemRequirement",
                ["SoftwareRequirement"] = "SoftwareRequirement",
                ["DocumentationRequirement"] = "Documentation",
                ["DocContent"] = "DocContent",
                ["SoftwareSystemTest"] = "SoftwareSystemTest",
                ["Anomaly"] = "Bug",
                ["Risk"] = "Risk",
                ["SOUP"] = "SOUP"
            };
            SetupTruthItemConfigurations(configTable, trackerNames);
            SetupMockFileSystem();
            SetupMockConfiguration();
            SetupMockRedmineResponses();

            // Act & Assert
            ClassicAssert.DoesNotThrow(() => InitializePluginWithConfiguration(configTable));
        }

        [UnitTestAttribute(
        Identifier = "A6FD94C7-1850-4C88-95F0-F7A4B9EE7225",
        Purpose = "RedmineSLMSPlugin throws when required configuration is missing",
        PostCondition = "Exception is thrown for missing required configuration")]
        [Test]
        public void TestRedmineSLMSPluginMissingConfiguration()
        {
            // Arrange
            var configTable = new TomlTable();
            configTable["RedmineAPIEndpoint"] = "http://localhost:3001/";
            // Missing RedmineAPIKey and other required fields

            SetupMockFileSystem();

            // Act & Assert
            ClassicAssert.Throws<Exception>(() => InitializePluginWithConfiguration(configTable));
        }

        [UnitTestAttribute(
        Identifier = "6F838C42-D8E4-4FA5-8147-30E87C7633CB",
        Purpose = "ConfigureServices registers IRedmineClient correctly",
        PostCondition = "IRedmineClient is registered in the service collection")]
        [Test]
        public void TestConfigureServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var configTable = CreateBaseConfiguration();
            var trackerNames = new Dictionary<string, string>
            {
                ["SystemRequirement"] = "SystemRequirement",
                ["SoftwareRequirement"] = "SoftwareRequirement",
                ["DocumentationRequirement"] = "Documentation",
                ["DocContent"] = "DocContent",
                ["SoftwareSystemTest"] = "SoftwareSystemTest",
                ["Anomaly"] = "Bug",
                ["Risk"] = "Risk",
                ["SOUP"] = "SOUP"
            };
            SetupTruthItemConfigurations(configTable, trackerNames);
            SetupMockFileSystem();
            SetupMockConfiguration();
            fileSystem.File.ReadAllText(Arg.Any<string>()).Returns(ConvertTomlTableToString(configTable));
            services.AddSingleton(configuration);
            services.AddSingleton(fileProviderPlugin);

            // Act
            plugin.ConfigureServices(services);

            // Assert
            var provider = services.BuildServiceProvider();
            var resolvedClient = provider.GetService<IRedmineClient>();
            ClassicAssert.IsNotNull(resolvedClient);
        }

        [UnitTestAttribute(
        Identifier = "4D9550F5-A133-46C8-A886-62AC7994A451",
        Purpose = "RedmineSLMSPlugin works with mocked IRedmineClient",
        PostCondition = "Plugin is able to refresh items using the mocked client")]
        [Test]
        public void TestRedmineSLMSPluginWithMockedClient()
        {
            // Arrange
            var configTable = CreateBaseConfiguration();
            var trackerNames = new Dictionary<string, string>
            {
                ["SystemRequirement"] = "SystemRequirement",
                ["SoftwareRequirement"] = "SoftwareRequirement",
                ["DocumentationRequirement"] = "Documentation",
                ["DocContent"] = "DocContent",
                ["SoftwareSystemTest"] = "SoftwareSystemTest",
                ["Anomaly"] = "Bug",
                ["Risk"] = "Risk",
                ["SOUP"] = "SOUP"
            };
            SetupTruthItemConfigurations(configTable, trackerNames);
            SetupMockFileSystem();
            SetupMockConfiguration();
            SetupMockRedmineResponses();

            // Setup mock responses for issues
            var initialIssuesResponse = new RedmineIssues
            {
                Issues = new List<RedmineIssue>
                {
                    new RedmineIssue
                    {
                        Id = 1,
                        Subject = "System Requirement 1",
                        Description = "Test Description",
                        Status = new Status { Id = 1, Name = "New" },
                        Tracker = new RedmineTracker { Id = 1, Name = "Risk" },
                        Relations = new List<Relation>
                        {
                            new Relation { IssueId = 2, RelationType = "relates" }
                        },
                        UpdatedOn = DateTime.UtcNow,
                        AssignedTo = new AssignedTo { Id = 1, Name = "Test User" },
                        CustomFields = new List<CustomField>
                        {
                            new CustomField 
                            { 
                                Id = 1, 
                                Name = "Version", 
                                Value = System.Text.Json.JsonSerializer.SerializeToElement("1.0"),
                                Multiple = false
                            }
                        },
                        Project = new RedmineProject { Id = 1, Name = "TestProject" }
                    }
                }
            };

            redmineClient.GetAsync<RedmineIssues>(Arg.Any<RestRequest>())
                .Returns(initialIssuesResponse);

            InitializePluginWithConfiguration(configTable);

            // Act & Assert
            ClassicAssert.DoesNotThrow(() => plugin.RefreshItems());
        }

        [UnitTestAttribute(
        Identifier = "C9D0E1F2-A345-6789-C345-90123ABCDEF1",
        Purpose = "RedmineSLMSPlugin throws exception when trying to pull issues for non-existent tracker",
        PostCondition = "Appropriate exception is thrown with descriptive message")]
        [Test]
        public void TestRedmineSLMSPluginNonExistentTracker()
        {
            // Arrange
            var configTable = CreateBaseConfiguration();
            var trackerNames = new Dictionary<string, string>
            {
                ["SystemRequirement"] = "NonExistentTracker",
                ["SoftwareRequirement"] = "SoftwareRequirement",
                ["DocumentationRequirement"] = "Documentation",
                ["DocContent"] = "DocContent",
                ["SoftwareSystemTest"] = "SoftwareSystemTest",
                ["Anomaly"] = "Bug",
                ["Risk"] = "Risk",
                ["SOUP"] = "SOUP"
            };
            SetupTruthItemConfigurations(configTable, trackerNames);
            SetupMockFileSystem();
            SetupMockConfiguration();
            SetupMockRedmineResponses();

            InitializePluginWithConfiguration(configTable);

            // Act & Assert
            var exception = ClassicAssert.Throws<Exception>(() => plugin.RefreshItems());
            ClassicAssert.That(exception.Message, Does.Contain("NonExistentTracker"));
            ClassicAssert.That(exception.Message, Does.Contain("not present"));
        }

        [UnitTestAttribute(
        Identifier = "75C51783-86D7-46DD-8F8E-68CD05736816",
        Purpose = "CreateTestCase correctly creates a SoftwareSystemTestItem from Redmine issues",
        PostCondition = "SoftwareSystemTestItem is created with correct properties from Redmine issues")]
        [Test]
        public void TestCreateTestCase()
        {
            // Arrange
            var issues = new List<RedmineIssue>
            {
                new RedmineIssue
                {
                    Id = 1,
                    Subject = "Software Requirement",
                    Description = "Software Requirement description",
                    Status = new Status { Id = 1, Name = "New" },
                    Tracker = new RedmineTracker { Id = 1, Name = "SoftwareRequirement" },
                    Relations = new List<Relation>
                    {
                        new Relation { IssueId = 2, RelationType = "relates" }
                    },
                    UpdatedOn = DateTime.UtcNow,
                    AssignedTo = new AssignedTo { Id = 1, Name = "Test User" },
                    CustomFields = new List<CustomField>
                    {
                        new CustomField
                        {
                            Id = 1,
                            Name = "Version",
                            Value = System.Text.Json.JsonSerializer.SerializeToElement("1.0"),
                            Multiple = false
                        }
                    },
                    Project = new RedmineProject { Id = 1, Name = "TestProject" }
                }
            };

            var testCaseIssue = new RedmineIssue
            {
                Id = 2,
                Subject = "Test Case 2",
                Description = "Test case description with steps:\n1. First step\n2. Second step\n3. Expected result",
                Status = new Status { Id = 1, Name = "New" },
                Tracker = new RedmineTracker { Id = 1, Name = "SoftwareSystemTest" },
                Relations = new List<Relation>
                {
                    new Relation { IssueId = 1, RelationType = "relates" }
                },
                UpdatedOn = DateTime.UtcNow,
                AssignedTo = new AssignedTo { Id = 1, Name = "Test User" },
                FixedVersion = new FixedVersion { Id = 1, Name = "theFixedVersion" },
                CustomFields = new List<CustomField>
                {
                    new CustomField 
                    { 
                        Id = 1, 
                        Name = "Test Method", 
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("Unit Tested"),
                        Multiple = false
                    },
                    new CustomField
                    {
                        Id = 2,
                        Name = "Identifier",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("UnitTestIdentifier"),
                        Multiple = false
                    }
                },
                Project = new RedmineProject { Id = 1, Name = "TestProject" }
            };

            var configTable = CreateBaseConfiguration();
            var trackerNames = new Dictionary<string, string>
            {
                ["SystemRequirement"] = "NonExistentTracker",
                ["SoftwareRequirement"] = "SoftwareRequirement",
                ["DocumentationRequirement"] = "Documentation",
                ["DocContent"] = "DocContent",
                ["SoftwareSystemTest"] = "SoftwareSystemTest",
                ["Anomaly"] = "Bug",
                ["Risk"] = "Risk",
                ["SOUP"] = "SOUP"
            };
            SetupTruthItemConfigurations(configTable, trackerNames);
            SetupMockConfiguration();
            SetupMockFileSystem();
            InitializePluginWithConfiguration(configTable);

            // Act
            var testCase = plugin.CreateTestCase(issues, testCaseIssue);

            // Assert
            ClassicAssert.NotNull(testCase);
            ClassicAssert.AreEqual("2", testCase.ItemID);
            ClassicAssert.AreEqual("Test Case 2", testCase.ItemTitle);
            ClassicAssert.AreEqual(4,testCase.TestCaseSteps.Count());
            ClassicAssert.AreEqual(2, testCase.LinkedItems.Count());
            ClassicAssert.AreEqual("1", testCase.LinkedItems.Last().TargetID);
            ClassicAssert.AreEqual("UnitTestIdentifier", testCase.LinkedItems.First().TargetID);
            ClassicAssert.AreEqual(ItemLinkType.Tests, testCase.LinkedItems.Last().LinkType);
            ClassicAssert.AreEqual(ItemLinkType.UnitTest, testCase.LinkedItems.First().LinkType);
            ClassicAssert.AreEqual("New", testCase.ItemStatus);
            ClassicAssert.AreEqual(true, testCase.TestCaseAutomated);
            ClassicAssert.AreEqual(true, testCase.TestCaseToUnitTest);
            ClassicAssert.AreEqual("theFixedVersion", testCase.ItemTargetVersion);
        }

        [UnitTestAttribute(
        Identifier = "D0C3D04C-1AD4-4447-9380-E5DD5D0C5DE5",
        Purpose = "CreateDocContent correctly creates a DocContentItem from Redmine issues",
        PostCondition = "DocContentItem is created with correct properties from Redmine issues")]
        [Test]
        public void TestCreateDocContent()
        {
            // Arrange
            var docContentIssue = new RedmineIssue
            {
                Id = 2,
                Subject = "Doc Content 2",
                Description = "Documentation content with sections:\n1. Introduction\n2. Main Content\n3. Conclusion",
                Status = new Status { Id = 1, Name = "New" },
                Tracker = new RedmineTracker { Id = 1, Name = "DocContent" },
                Relations = new List<Relation>
                {
                    new Relation { IssueId = 1, RelationType = "relates" }
                },
                UpdatedOn = DateTime.UtcNow,
                AssignedTo = new AssignedTo { Id = 1, Name = "Test User" },
                FixedVersion = new FixedVersion { Id = 1, Name = "theFixedVersion" },
                CustomFields = new List<CustomField>
                {
                    new CustomField 
                    { 
                        Id = 1, 
                        Name = "Functional Area", 
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("the functional area"),
                        Multiple = false
                    },
                },
                Project = new RedmineProject { Id = 1, Name = "TestProject" }
            };

            var configTable = CreateBaseConfiguration();
            var trackerNames = new Dictionary<string, string>
            {
                ["SystemRequirement"] = "SystemRequirement",
                ["SoftwareRequirement"] = "SoftwareRequirement",
                ["DocumentationRequirement"] = "Documentation",
                ["DocContent"] = "DocContent",
                ["SoftwareSystemTest"] = "SoftwareSystemTest",
                ["Anomaly"] = "Bug",
                ["Risk"] = "Risk",
                ["SOUP"] = "SOUP"
            };
            SetupTruthItemConfigurations(configTable, trackerNames);
            SetupMockConfiguration();
            SetupMockFileSystem();
            InitializePluginWithConfiguration(configTable);

            // Act
            var docContent = plugin.CreateDocContent(docContentIssue);

            // Assert
            ClassicAssert.NotNull(docContent);
            ClassicAssert.AreEqual("2", docContent.ItemID);
            ClassicAssert.AreEqual("Documentation content with sections:\n1. Introduction\n2. Main Content\n3. Conclusion", docContent.DocContent);
            ClassicAssert.AreEqual(1, docContent.LinkedItems.Count());
            ClassicAssert.AreEqual("1", docContent.LinkedItems.First().TargetID);
            ClassicAssert.AreEqual(ItemLinkType.Related, docContent.LinkedItems.First().LinkType);
            ClassicAssert.AreEqual("New", docContent.ItemStatus);
            ClassicAssert.AreEqual("theFixedVersion", docContent.ItemTargetVersion);
            ClassicAssert.AreEqual("the functional area", docContent.ItemCategory);
        }

        [UnitTestAttribute(
        Identifier = "D6E7F8G9-0123-4567-D012-678901AB23BV",
        Purpose = "CreateSOUP correctly creates a SOUPItem from Redmine issues",
        PostCondition = "SOUPItem is created with correct properties from Redmine issues")]
        [Test]
        public void TestCreateSOUP()
        {
            // Arrange
            var soupIssue = new RedmineIssue
            {
                Id = 2,
                Subject = "SOUP Item 2",
                Description = "SOUP item details:\n1. Purpose\n2. Usage\n3. Dependencies",
                Status = new Status { Id = 1, Name = "New" },
                Tracker = new RedmineTracker { Id = 1, Name = "SOUP" },
                Relations = new List<Relation>
                {
                    new Relation { IssueId = 1, RelationType = "relates" }
                },
                UpdatedOn = DateTime.UtcNow,
                AssignedTo = new AssignedTo { Id = 1, Name = "Test User" },
                FixedVersion = new FixedVersion { Id = 1, Name = "theFixedVersion" },
                CustomFields = new List<CustomField>
                {
                    new CustomField
                    {
                        Id = 2,
                        Name = "Version",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("1.0.0"),
                        Multiple = false
                    },
                    new CustomField
                    {
                        Id = 3,
                        Name = "Linked Library",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("1"),
                        Multiple = false
                    },
                    new CustomField
                    {
                        Id = 4,
                        Name = "SOUP Detailed Description",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("Detailed description of the SOUP component"),
                        Multiple = false
                    },
                    new CustomField
                    {
                        Id = 5,
                        Name = "Performance Critical?",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("This component is performance critical"),
                        Multiple = false
                    },
                    new CustomField
                    {
                        Id = 6,
                        Name = "CyberSecurity Critical?",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("This component is not cybersecurity critical"),
                        Multiple = false
                    },
                    new CustomField
                    {
                        Id = 7,
                        Name = "Anomaly List Examination",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("List of known anomalies in the component"),
                        Multiple = false
                    },
                    new CustomField
                    {
                        Id = 8,
                        Name = "Installed by end user?",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("Yes, installed by end user"),
                        Multiple = false
                    },
                    new CustomField
                    {
                        Id = 9,
                        Name = "End user training",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("Required training for end users"),
                        Multiple = false
                    },
                    new CustomField
                    {
                        Id = 10,
                        Name = "SOUP License",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("MIT"),
                        Multiple = false
                    },
                    new CustomField
                    {
                        Id = 11,
                        Name = "Manufacturer",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("Test Manufacturer"),
                        Multiple = false
                    }
                },
                Project = new RedmineProject { Id = 1, Name = "TestProject" }
            };

            var configTable = CreateBaseConfiguration();
            var trackerNames = new Dictionary<string, string>
            {
                ["SystemRequirement"] = "SystemRequirement",
                ["SoftwareRequirement"] = "SoftwareRequirement",
                ["DocumentationRequirement"] = "Documentation",
                ["DocContent"] = "DocContent",
                ["SoftwareSystemTest"] = "SoftwareSystemTest",
                ["Anomaly"] = "Bug",
                ["Risk"] = "Risk",
                ["SOUP"] = "SOUP"
            };
            SetupTruthItemConfigurations(configTable, trackerNames);
            SetupMockConfiguration();
            SetupMockFileSystem();
            InitializePluginWithConfiguration(configTable);

            // Act
            var soup = plugin.CreateSOUP(soupIssue);

            // Assert
            ClassicAssert.NotNull(soup);
            ClassicAssert.AreEqual("2", soup.ItemID);
            ClassicAssert.AreEqual("Detailed description of the SOUP component", soup.SOUPDetailedDescription);
            ClassicAssert.AreEqual(1, soup.LinkedItems.Count());
            ClassicAssert.AreEqual("1", soup.LinkedItems.First().TargetID);
            ClassicAssert.AreEqual(ItemLinkType.Related, soup.LinkedItems.First().LinkType);
            ClassicAssert.AreEqual("New", soup.ItemStatus);
            ClassicAssert.AreEqual("theFixedVersion", soup.ItemTargetVersion);
            ClassicAssert.AreEqual("SOUP Item 2", soup.SOUPName);
            ClassicAssert.AreEqual("1.0.0", soup.SOUPVersion);
            ClassicAssert.AreEqual(true, soup.SOUPLinkedLib);
            ClassicAssert.AreEqual(true, soup.SOUPPerformanceCritical);
            ClassicAssert.AreEqual("This component is performance critical", soup.SOUPPerformanceCriticalText);
            ClassicAssert.AreEqual(false, soup.SOUPCybersecurityCritical);
            ClassicAssert.AreEqual("This component is not cybersecurity critical", soup.SOUPCybersecurityCriticalText);
            ClassicAssert.AreEqual("List of known anomalies in the component", soup.SOUPAnomalyListDescription);
            ClassicAssert.AreEqual(true, soup.SOUPInstalledByUser);
            ClassicAssert.AreEqual("Yes, installed by end user", soup.SOUPInstalledByUserText);
            ClassicAssert.AreEqual("Required training for end users", soup.SOUPEnduserTraining);
            ClassicAssert.AreEqual("MIT", soup.SOUPLicense);
            ClassicAssert.AreEqual("Test Manufacturer", soup.SOUPManufacturer);
        }

        [UnitTestAttribute(
        Identifier = "E7F8GMND-1234-5678-E123-456789ABCDEF",
        Purpose = "CreateBug correctly creates an AnomalyItem from Redmine issues",
        PostCondition = "AnomalyItem is created with correct properties from Redmine issues")]
        [Test]
        public void TestCreateBug()
        {
            // Arrange
            var bugIssue = new RedmineIssue
            {
                Id = 2,
                Subject = "Bug Title",
                Description = "Detailed bug description with steps to reproduce",
                Status = new Status { Id = 1, Name = "New" },
                Tracker = new RedmineTracker { Id = 1, Name = "Bug" },
                Relations = new List<Relation>
                {
                    new Relation { IssueId = 1, RelationType = "relates" }
                },
                UpdatedOn = DateTime.UtcNow,
                AssignedTo = new AssignedTo { Id = 1, Name = "Test User" },
                FixedVersion = new FixedVersion { Id = 1, Name = "theFixedVersion" },
                CustomFields = new List<CustomField>
                {
                    new CustomField 
                    { 
                        Id = 1, 
                        Name = "Justification", 
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("Bug justification text"),
                        Multiple = false
                    },
                    new CustomField
                    {
                        Id = 2,
                        Name = "Severity",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("High"),
                        Multiple = false
                    }
                },
                Project = new RedmineProject { Id = 1, Name = "TestProject" }
            };

            var configTable = CreateBaseConfiguration();
            var trackerNames = new Dictionary<string, string>
            {
                ["SystemRequirement"] = "SystemRequirement",
                ["SoftwareRequirement"] = "SoftwareRequirement",
                ["DocumentationRequirement"] = "Documentation",
                ["DocContent"] = "DocContent",
                ["SoftwareSystemTest"] = "SoftwareSystemTest",
                ["Anomaly"] = "Bug",
                ["Risk"] = "Risk",
                ["SOUP"] = "SOUP"
            };
            SetupTruthItemConfigurations(configTable, trackerNames);
            SetupMockConfiguration();
            SetupMockFileSystem();
            InitializePluginWithConfiguration(configTable);

            // Act
            var bug = plugin.CreateBug(bugIssue);

            // Assert
            ClassicAssert.NotNull(bug);
            ClassicAssert.AreEqual("2", bug.ItemID);
            ClassicAssert.AreEqual("Bug Title", bug.ItemTitle);
            ClassicAssert.AreEqual("Detailed bug description with steps to reproduce", bug.AnomalyDetailedDescription);
            ClassicAssert.AreEqual(1, bug.LinkedItems.Count());
            ClassicAssert.AreEqual("1", bug.LinkedItems.First().TargetID);
            ClassicAssert.AreEqual(ItemLinkType.Related, bug.LinkedItems.First().LinkType);
            ClassicAssert.AreEqual("New", bug.ItemStatus);
            ClassicAssert.AreEqual("New", bug.AnomalyState);
            ClassicAssert.AreEqual("theFixedVersion", bug.ItemTargetVersion);
            ClassicAssert.AreEqual("Test User", bug.AnomalyAssignee);
            ClassicAssert.AreEqual("Bug justification text", bug.AnomalyJustification);
            ClassicAssert.AreEqual("High", bug.AnomalySeverity);
            ClassicAssert.AreEqual(bugIssue.UpdatedOn.ToString(), bug.ItemRevision);
            ClassicAssert.AreEqual(bugIssue.UpdatedOn, bug.ItemLastUpdated);
            ClassicAssert.AreEqual(new Uri("http://localhost:3001/issues/2"), bug.Link);
        }

        [UnitTestAttribute(
        Identifier = "F8G9H0I1-2345-6789-F234-56789023CDEF",
        Purpose = "CreateRisk correctly creates a RiskItem from Redmine issues",
        PostCondition = "RiskItem is created with correct properties from Redmine issues")]
        [Test]
        public void TestCreateRisk()
        {
            // Arrange
            var riskIssue = new RedmineIssue
            {
                Id = 2,
                Subject = "Risk Title",
                Description = "Detailed risk description with failure mode analysis",
                Status = new Status { Id = 1, Name = "New" },
                Tracker = new RedmineTracker { Id = 1, Name = "Risk" },
                Relations = new List<Relation>
                {
                    new Relation { IssueId = 1, RelationType = "relates" }
                },
                UpdatedOn = DateTime.UtcNow,
                AssignedTo = new AssignedTo { Id = 1, Name = "Test User" },
                FixedVersion = new FixedVersion { Id = 1, Name = "theFixedVersion" },
                CustomFields = new List<CustomField>
                {
                    new CustomField 
                    { 
                        Id = 1, 
                        Name = "Risk Type", 
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("Technical"),
                        Multiple = false
                    },
                    new CustomField
                    {
                        Id = 2,
                        Name = "Risk",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("System failure"),
                        Multiple = false
                    },
                    new CustomField
                    {
                        Id = 3,
                        Name = "Hazard Severity",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("5-Critical"),
                        Multiple = false
                    },
                    new CustomField
                    {
                        Id = 4,
                        Name = "Hazard Probability",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("4-High"),
                        Multiple = false
                    },
                    new CustomField
                    {
                        Id = 5,
                        Name = "Hazard Detectability",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("3-Medium"),
                        Multiple = false
                    },
                    new CustomField
                    {
                        Id = 6,
                        Name = "Residual Probability",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("2-Low"),
                        Multiple = false
                    },
                    new CustomField
                    {
                        Id = 7,
                        Name = "Residual Detectability",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("1-Very Low"),
                        Multiple = false
                    },
                    new CustomField
                    {
                        Id = 8,
                        Name = "Risk Control Category",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("Prevention\tAdditional info"),
                        Multiple = false
                    },
                    new CustomField
                    {
                        Id = 9,
                        Name = "Detection Method",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("Automated testing"),
                        Multiple = false
                    }
                },
                Project = new RedmineProject { Id = 1, Name = "TestProject" }
            };

            var controlIssue = new RedmineIssue
            {
                Id = 1,
                Subject = "Control Measure",
                Description = "Implementation details of the control measure",
                Status = new Status { Id = 1, Name = "New" },
                Tracker = new RedmineTracker { Id = 1, Name = "SystemRequirement" }
            };

            var controlIssues = new RedmineIssues() { Issues = new List<RedmineIssue> { controlIssue } };

            redmineClient.GetAsync<RedmineIssues>(Arg.Any<RestRequest>())
                .Returns(controlIssues);

            var configTable = CreateBaseConfiguration();
            var trackerNames = new Dictionary<string, string>
            {
                ["SystemRequirement"] = "SystemRequirement",
                ["SoftwareRequirement"] = "SoftwareRequirement",
                ["DocumentationRequirement"] = "Documentation",
                ["DocContent"] = "DocContent",
                ["SoftwareSystemTest"] = "SoftwareSystemTest",
                ["Anomaly"] = "Bug",
                ["Risk"] = "Risk",
                ["SOUP"] = "SOUP"
            };
            SetupTruthItemConfigurations(configTable, trackerNames);
            SetupMockConfiguration();
            SetupMockFileSystem();
            InitializePluginWithConfiguration(configTable);

            // Act
            var risk = plugin.CreateRisk(new List<RedmineIssue> { controlIssue, riskIssue }, riskIssue);

            // Assert
            ClassicAssert.NotNull(risk);
            ClassicAssert.AreEqual("2", risk.ItemID);
            ClassicAssert.AreEqual("Risk Title", risk.RiskFailureMode);
            ClassicAssert.AreEqual("Detailed risk description with failure mode analysis", risk.RiskCauseOfFailure);
            ClassicAssert.AreEqual(1, risk.LinkedItems.Count());
            ClassicAssert.AreEqual("1", risk.LinkedItems.First().TargetID);
            ClassicAssert.AreEqual(ItemLinkType.RiskControl, risk.LinkedItems.First().LinkType);
            ClassicAssert.AreEqual("New", risk.ItemStatus);
            ClassicAssert.AreEqual("theFixedVersion", risk.ItemTargetVersion);
            ClassicAssert.AreEqual("Technical", risk.ItemCategory);
            ClassicAssert.AreEqual("System failure", risk.RiskPrimaryHazard);
            ClassicAssert.AreEqual(5, risk.RiskSeverityScore);
            ClassicAssert.AreEqual(4, risk.RiskOccurenceScore);
            ClassicAssert.AreEqual(3, risk.RiskDetectabilityScore);
            ClassicAssert.AreEqual(2, risk.RiskModifiedOccScore);
            ClassicAssert.AreEqual(1, risk.RiskModifiedDetScore);
            ClassicAssert.AreEqual("Prevention", risk.RiskControlMeasureType);
            ClassicAssert.AreEqual("Automated testing", risk.RiskMethodOfDetection);
            ClassicAssert.AreEqual("Control Measure", risk.RiskControlMeasure);
            ClassicAssert.AreEqual("Implementation details of the control measure", risk.RiskControlImplementation);
            ClassicAssert.AreEqual(riskIssue.UpdatedOn.ToString(), risk.ItemRevision);
            ClassicAssert.AreEqual(riskIssue.UpdatedOn, risk.ItemLastUpdated);
            ClassicAssert.AreEqual(new Uri("http://localhost:3001/issues/2"), risk.Link);
        }

        [UnitTestAttribute(
        Identifier = "G9H0I1J2-3456-7890-G345-678901A8XDEF",
        Purpose = "CreateRisk throws exception when risk has multiple related issues",
        PostCondition = "Exception is thrown with appropriate message when risk has more than one related issue")]
        [Test]
        public void TestCreateRiskMultipleRelatedIssues()
        {
            // Arrange
            var riskIssue = new RedmineIssue
            {
                Id = 2,
                Subject = "Risk Title",
                Description = "Detailed risk description with failure mode analysis",
                Status = new Status { Id = 1, Name = "New" },
                Tracker = new RedmineTracker { Id = 1, Name = "Risk" },
                Relations = new List<Relation>
                {
                    new Relation { IssueId = 1, RelationType = "relates" },
                    new Relation { IssueId = 3, RelationType = "relates" }
                },
                UpdatedOn = DateTime.UtcNow,
                AssignedTo = new AssignedTo { Id = 1, Name = "Test User" },
                FixedVersion = new FixedVersion { Id = 1, Name = "theFixedVersion" },
                CustomFields = new List<CustomField>
                {
                    new CustomField 
                    { 
                        Id = 1, 
                        Name = "Risk Type", 
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("Technical"),
                        Multiple = false
                    },
                    new CustomField
                    {
                        Id = 2,
                        Name = "Risk",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("System failure"),
                        Multiple = false
                    }
                },
                Project = new RedmineProject { Id = 1, Name = "TestProject" }
            };

            var controlIssue1 = new RedmineIssue
            {
                Id = 1,
                Subject = "Control Measure 1",
                Description = "Implementation details of the first control measure",
                Status = new Status { Id = 1, Name = "New" },
                Tracker = new RedmineTracker { Id = 1, Name = "SystemRequirement" }
            };

            var controlIssue2 = new RedmineIssue
            {
                Id = 3,
                Subject = "Control Measure 2",
                Description = "Implementation details of the second control measure",
                Status = new Status { Id = 1, Name = "New" },
                Tracker = new RedmineTracker { Id = 1, Name = "SystemRequirement" }
            };

            var controlIssues = new RedmineIssues() 
            { 
                Issues = new List<RedmineIssue> { controlIssue1, controlIssue2 } 
            };

            redmineClient.GetAsync<RedmineIssues>(Arg.Any<RestRequest>())
                .Returns(controlIssues);

            var configTable = CreateBaseConfiguration();
            var trackerNames = new Dictionary<string, string>
            {
                ["SystemRequirement"] = "SystemRequirement",
                ["SoftwareRequirement"] = "SoftwareRequirement",
                ["DocumentationRequirement"] = "Documentation",
                ["DocContent"] = "DocContent",
                ["SoftwareSystemTest"] = "SoftwareSystemTest",
                ["Anomaly"] = "Bug",
                ["Risk"] = "Risk",
                ["SOUP"] = "SOUP"
            };
            SetupTruthItemConfigurations(configTable, trackerNames);
            SetupMockConfiguration();
            SetupMockFileSystem();
            InitializePluginWithConfiguration(configTable);

            // Act & Assert
            var exception = ClassicAssert.Throws<Exception>(() => 
                plugin.CreateRisk(new List<RedmineIssue> { controlIssue1, controlIssue2, riskIssue }, riskIssue));
            ClassicAssert.That(exception.Message, Does.Contain("Only a single related link to risk item"));
            ClassicAssert.That(exception.Message, Does.Contain("2"));
        }

        [UnitTestAttribute(
        Identifier = "H0I1J2K3-4567-8901-H456-789012ABCDEF",
        Purpose = "CreateRequirement correctly creates a RequirementItem with proper properties and child links",
        PostCondition = "RequirementItem is created with correct properties and child links for issues that reference it as their parent")]
        [Test]
        public void TestCreateRequirement()
        {
            // Arrange
            var requirementIssue = new RedmineIssue
            {
                Id = 1,
                Subject = "Parent Requirement",
                Description = "Parent requirement description",
                Status = new Status { Id = 1, Name = "New" },
                Tracker = new RedmineTracker { Id = 1, Name = "SystemRequirement" },
                UpdatedOn = DateTime.UtcNow,
                AssignedTo = new AssignedTo { Id = 1, Name = "Test User" },
                FixedVersion = new FixedVersion { Id = 1, Name = "theFixedVersion" },
                CustomFields = new List<CustomField>
                {
                    new CustomField
                    {
                        Id = 1,
                        Name = "Functional Area",
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("Test Area"),
                        Multiple = false
                    }
                },
                Relations = new List<Relation>(),
                Project = new RedmineProject { Id = 1, Name = "TestProject" }
            };

            var childIssue1 = new RedmineIssue
            {
                Id = 2,
                Subject = "Child Requirement 1",
                Description = "First child requirement",
                Status = new Status { Id = 1, Name = "New" },
                Tracker = new RedmineTracker { Id = 1, Name = "SoftwareRequirement" },
                Parent = new Parent { Id = 1, Name = "Parent Requirement" },
                Project = new RedmineProject { Id = 1, Name = "TestProject" }
            };

            var childIssue2 = new RedmineIssue
            {
                Id = 3,
                Subject = "Child Requirement 2",
                Description = "Second child requirement",
                Status = new Status { Id = 1, Name = "New" },
                Tracker = new RedmineTracker { Id = 1, Name = "SoftwareRequirement" },
                Parent = new Parent { Id = 1, Name = "Parent Requirement" },
                Project = new RedmineProject { Id = 1, Name = "TestProject" }
            };

            var unrelatedIssue = new RedmineIssue
            {
                Id = 4,
                Subject = "Unrelated Requirement",
                Description = "This requirement is not a child",
                Status = new Status { Id = 1, Name = "New" },
                Tracker = new RedmineTracker { Id = 1, Name = "SoftwareRequirement" },
                Parent = new Parent { Id = 5, Name = "Different Parent" },
                Project = new RedmineProject { Id = 1, Name = "TestProject" }
            };

            var configTable = CreateBaseConfiguration();
            var trackerNames = new Dictionary<string, string>
            {
                ["SystemRequirement"] = "SystemRequirement",
                ["SoftwareRequirement"] = "SoftwareRequirement",
                ["DocumentationRequirement"] = "Documentation",
                ["DocContent"] = "DocContent",
                ["SoftwareSystemTest"] = "SoftwareSystemTest",
                ["Anomaly"] = "Bug",
                ["Risk"] = "Risk",
                ["SOUP"] = "SOUP"
            };
            SetupTruthItemConfigurations(configTable, trackerNames);
            SetupMockConfiguration();
            SetupMockFileSystem();
            InitializePluginWithConfiguration(configTable);

            // Act
            var requirement = plugin.CreateRequirement(
                new List<RedmineIssue> { requirementIssue, childIssue1, childIssue2, unrelatedIssue }, 
                requirementIssue, 
                RequirementType.SystemRequirement);

            // Assert
            ClassicAssert.NotNull(requirement);
            
            // Verify basic properties
            ClassicAssert.AreEqual("1", requirement.ItemID);
            ClassicAssert.AreEqual("Parent Requirement", requirement.ItemTitle);
            ClassicAssert.AreEqual("Parent requirement description", requirement.RequirementDescription);
            ClassicAssert.AreEqual("Test Area", requirement.ItemCategory);
            ClassicAssert.AreEqual("New", requirement.ItemStatus);
            ClassicAssert.AreEqual("theFixedVersion", requirement.ItemTargetVersion);
            ClassicAssert.AreEqual("Test User", requirement.RequirementAssignee);
            ClassicAssert.AreEqual(new Uri("http://localhost:3001/issues/1"), requirement.Link);
            ClassicAssert.AreEqual(requirementIssue.UpdatedOn.ToString(), requirement.ItemRevision);
            ClassicAssert.AreEqual(requirementIssue.UpdatedOn, requirement.ItemLastUpdated);

            // Verify child links
            ClassicAssert.AreEqual(2, requirement.LinkedItems.Count());
            ClassicAssert.IsTrue(requirement.LinkedItems.Any(l => l.TargetID == "2" && l.LinkType == ItemLinkType.Child));
            ClassicAssert.IsTrue(requirement.LinkedItems.Any(l => l.TargetID == "3" && l.LinkType == ItemLinkType.Child));
            ClassicAssert.IsFalse(requirement.LinkedItems.Any(l => l.TargetID == "4"));
        }

        #endregion

        #region Custom Field Filtering Tests

        [UnitTestAttribute(
        Identifier = "I1J2K3L4-5678-9012-I567-890123ABCDEF",
        Purpose = "ShouldIgnoreIssue correctly filters items based on custom field values",
        PostCondition = "Items are correctly filtered based on custom field inclusion and exclusion rules")]
        [Test]
        public void TestShouldIgnoreIssueCustomFieldFiltering()
        {
            // Arrange
            var redmineIssue = new RedmineIssue
            {
                Id = 1,
                Subject = "Test Issue",
                Status = new Status { Name = "New" },
                Tracker = new RedmineTracker { Name = "SystemRequirement" },
                CustomFields = new List<CustomField>
                {
                    new CustomField 
                    { 
                        Id = 1, 
                        Name = "Priority", 
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("High"),
                        Multiple = false
                    },
                    new CustomField 
                    { 
                        Id = 2, 
                        Name = "Tags", 
                        Value = (System.Text.Json.JsonSerializer.SerializeToElement(new[] { "feature", "important" })).EnumerateArray(),
                        Multiple = true
                    },
                    new CustomField 
                    { 
                        Id = 3, 
                        Name = "Version", 
                        Value = System.Text.Json.JsonSerializer.SerializeToElement("1.0"),
                        Multiple = false
                    }
                }
            };

            var config = new TruthItemConfig("SystemRequirement", true);

            // Setup configuration with filters
            var configTable = CreateBaseConfiguration();

            // Add inclusion filter for Priority
            var includedFields = new TomlTable();
            var priorityValues = new TomlArray();
            priorityValues.Add("High");
            priorityValues.Add("Medium");
            includedFields["Priority"] = priorityValues;
            configTable["IncludedItemFilter"] = includedFields;

            // Add exclusion filter for Tags
            var excludedFields = new TomlTable();
            var tagValues = new TomlArray();
            tagValues.Add("deprecated");
            excludedFields["Tags"] = tagValues;
            configTable["ExcludedItemFilter"] = excludedFields;

            // Add version fields
            var versionFields = new TomlArray();
            versionFields.Add("Version");
            configTable["RedmineVersionFields"] = versionFields;

            // Add empty ignore list
            var ignoreList = new TomlArray();
            configTable["Ignore"] = ignoreList;

            // Add all required truth item configurations
            var trackerNames = new Dictionary<string, string>
            {
                ["SystemRequirement"] = "SystemRequirement",
                ["SoftwareRequirement"] = "SoftwareRequirement",
                ["DocumentationRequirement"] = "Documentation",
                ["DocContent"] = "DocContent",
                ["SoftwareSystemTest"] = "SoftwareSystemTest",
                ["Anomaly"] = "Bug",
                ["Risk"] = "Risk",
                ["SOUP"] = "SOUP"
            };
            SetupTruthItemConfigurations(configTable, trackerNames);
            SetupMockFileSystem();
            SetupMockConfiguration();
            SetupMockRedmineResponses();

            // Mock file system and configuration calls
            fileSystem.File.ReadAllText(Arg.Any<string>()).Returns(ConvertTomlTableToString(configTable));
            fileProviderPlugin = new LocalFileSystemPlugin(fileSystem);

            var plugin = new TestRedmineSLMSPlugin(fileProviderPlugin, redmineClient);
            plugin.InitializePlugin(configuration);

            // Act & Assert - Test 1: Should not ignore (matches inclusion, doesn't match exclusion)
            string reason;
            var shouldIgnore = plugin.ShouldIgnoreIssue(redmineIssue, config, out reason);
            ClassicAssert.IsFalse(shouldIgnore, "Item should not be ignored when it matches inclusion and doesn't match exclusion");

            // Test 2: Should ignore due to non-matching inclusion filter
            redmineIssue.CustomFields[0].Value = System.Text.Json.JsonSerializer.SerializeToElement("Low");
            shouldIgnore = plugin.ShouldIgnoreIssue(redmineIssue, config, out reason);
            ClassicAssert.IsTrue(shouldIgnore, "Item should be ignored when it doesn't match inclusion filter");
            ClassicAssert.That(reason, Does.Contain("Priority"));
            ClassicAssert.That(reason, Does.Contain("Low"));

            // Test 3: Should ignore due to matching exclusion filter
            redmineIssue.CustomFields[0].Value = System.Text.Json.JsonSerializer.SerializeToElement("High");
            redmineIssue.CustomFields[1].Value = (System.Text.Json.JsonSerializer.SerializeToElement(new[] { "deprecated", "important" })).EnumerateArray();
            shouldIgnore = plugin.ShouldIgnoreIssue(redmineIssue, config, out reason);
            ClassicAssert.IsTrue(shouldIgnore, "Item should be ignored when it matches exclusion filter");
            ClassicAssert.That(reason, Does.Contain("Tags"));
            ClassicAssert.That(reason, Does.Contain("deprecated"));

            // Test 4: Should not ignore with multiple values in inclusion filter
            redmineIssue.CustomFields[1].Value = (System.Text.Json.JsonSerializer.SerializeToElement(new[] { "feature", "important" })).EnumerateArray();
            shouldIgnore = plugin.ShouldIgnoreIssue(redmineIssue, config, out reason);
            ClassicAssert.IsFalse(shouldIgnore, "Item should not be ignored when multiple values match inclusion filter");

            // Test 5: Should handle version fields correctly
            redmineIssue.CustomFields[2].Value = System.Text.Json.JsonSerializer.SerializeToElement("2.0");
            shouldIgnore = plugin.ShouldIgnoreIssue(redmineIssue, config, out reason);
            ClassicAssert.IsFalse(shouldIgnore, "Item should not be ignored when version field is present");
        }

        #endregion

    }
}