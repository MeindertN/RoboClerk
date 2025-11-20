using NUnit.Framework;
using NSubstitute;
using RoboClerk.ContentCreators;
using RoboClerk.Core;
using RoboClerk.Core.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace RoboClerk.Tests
{
    [TestFixture]
    [Description("Tests for content creator metadata functionality")]
    public class TestContentCreatorMetadata
    {
        [UnitTestAttribute(
            Identifier = "0f2ad3de-7745-4bb1-a066-019f3277f172",
            Purpose = "Content creator metadata classes are created successfully",
            PostCondition = "No exception is thrown")]
        [Test]
        public void TestMetadataClassCreation()
        {
            var metadata = new ContentCreatorMetadata("SLMS", "Test Creator", "Test description");
            Assert.That(metadata.Source, Is.EqualTo("SLMS"));
            Assert.That(metadata.Name, Is.EqualTo("Test Creator"));
            Assert.That(metadata.Description, Is.EqualTo("Test description"));
            Assert.That(metadata.Tags, Is.Not.Null);
            Assert.That(metadata.Tags.Count, Is.EqualTo(0));
        }

        [UnitTestAttribute(
            Identifier = "d5a61a0b-7d43-4c5c-9936-a1d5376fae7b",
            Purpose = "Content creator tag can be created with parameters",
            PostCondition = "Tag is created with correct properties")]
        [Test]
        public void TestContentCreatorTagCreation()
        {
            var tag = new ContentCreatorTag("TestTag", "Test tag description");
            tag.Parameters.Add(new ContentCreatorParameter("param1", "First parameter", ParameterValueType.String, required: true));
            tag.Parameters.Add(new ContentCreatorParameter("param2", "Second parameter", ParameterValueType.Boolean, required: false, defaultValue: "true"));
            
            Assert.That(tag.TagID, Is.EqualTo("TestTag"));
            Assert.That(tag.Description, Is.EqualTo("Test tag description"));
            Assert.That(tag.Parameters.Count, Is.EqualTo(2));
            Assert.That(tag.Parameters[0].Name, Is.EqualTo("param1"));
            Assert.That(tag.Parameters[0].Required, Is.True);
            Assert.That(tag.Parameters[1].Name, Is.EqualTo("param2"));
            Assert.That(tag.Parameters[1].Required, Is.False);
            Assert.That(tag.Parameters[1].DefaultValue, Is.EqualTo("true"));
        }

        [UnitTestAttribute(
            Identifier = "8926d56d-d78f-4bad-8c91-ff0ee7c180d9",
            Purpose = "Document content creator returns valid metadata",
            PostCondition = "Metadata contains expected tags and parameters")]
        [Test]
        public void TestDocumentContentCreatorMetadata()
        {
            var traceAnalysis = Substitute.For<ITraceabilityAnalysis>();
            var document = new Document(traceAnalysis);
            
            var metadata = document.GetMetadata();
            
            Assert.That(metadata, Is.Not.Null);
            Assert.That(metadata.Source, Is.EqualTo("Document"));
            Assert.That(metadata.Name, Is.EqualTo("Document Properties"));
            Assert.That(metadata.Tags.Count, Is.GreaterThan(0));
            
            // Check for specific tags
            var titleTag = metadata.Tags.FirstOrDefault(t => t.TagID == "Title");
            Assert.That(titleTag, Is.Not.Null);
            Assert.That(titleTag.Description, Is.Not.Empty);
            
            var countEntitiesTag = metadata.Tags.FirstOrDefault(t => t.TagID == "CountEntities");
            Assert.That(countEntitiesTag, Is.Not.Null);
            Assert.That(countEntitiesTag.Parameters.Count, Is.GreaterThan(0));
            
            // Check parameters of countEntities
            var entityParam = countEntitiesTag.Parameters.FirstOrDefault(p => p.Name == "entity");
            Assert.That(entityParam, Is.Not.Null);
            Assert.That(entityParam.Required, Is.True);
            Assert.That(entityParam.ValueType, Is.EqualTo(ParameterValueType.EntityType));
        }

        [UnitTestAttribute(
            Identifier = "8bdbc7d2-9959-4b50-805f-8572c20644af",
            Purpose = "SOUP content creator returns valid metadata",
            PostCondition = "Metadata contains expected tags with parameters")]
        [Test]
        public void TestSOUPContentCreatorMetadata()
        {
            var dataSources = Substitute.For<IDataSources>();
            var traceAnalysis = Substitute.For<ITraceabilityAnalysis>();
            var config = Substitute.For<IConfiguration>();
            config.OutputFormat.Returns("ASCIIDOC");
            
            var soup = new SOUP(dataSources, traceAnalysis, config);
            
            var metadata = soup.GetMetadata();
            
            Assert.That(metadata, Is.Not.Null);
            Assert.That(metadata.Source, Is.EqualTo("SLMS"));
            Assert.That(metadata.Tags.Count, Is.GreaterThan(0));
            
            // Check that we have tags with brief and checkSOUP parameters
            var briefTag = metadata.Tags.FirstOrDefault(t => t.Parameters.Any(p => p.Name == "brief"));
            Assert.That(briefTag, Is.Not.Null);
            
            var checkTag = metadata.Tags.FirstOrDefault(t => t.Parameters.Any(p => p.Name == "checkSOUP"));
            Assert.That(checkTag, Is.Not.Null);
        }

        [UnitTestAttribute(
            Identifier = "d12453d1-384c-48d9-92a7-ed7550131a06",
            Purpose = "ExcelTable content creator returns valid metadata",
            PostCondition = "Metadata describes Excel import functionality")]
        [Test]
        public void TestExcelTableContentCreatorMetadata()
        {
            var dataSources = Substitute.For<IDataSources>();
            var traceAnalysis = Substitute.For<ITraceabilityAnalysis>();
            var config = Substitute.For<IConfiguration>();
            
            var excelTable = new ExcelTable(dataSources, traceAnalysis, config);
            
            var metadata = excelTable.GetMetadata();
            
            Assert.That(metadata, Is.Not.Null);
            Assert.That(metadata.Source, Is.EqualTo("FILE"));
            Assert.That(metadata.Category, Is.EqualTo("File Import"));
            Assert.That(metadata.Tags.Count, Is.GreaterThan(0));
            
            var tableTag = metadata.Tags.FirstOrDefault(t => t.TagID == "ExcelTable");
            Assert.That(tableTag, Is.Not.Null);
            
            // Check for required parameters
            var fileNameParam = tableTag.Parameters.FirstOrDefault(p => p.Name == "fileName");
            Assert.That(fileNameParam, Is.Not.Null);
            Assert.That(fileNameParam.Required, Is.True);
            Assert.That(fileNameParam.ValueType, Is.EqualTo(ParameterValueType.FilePath));
            
            var rangeParam = tableTag.Parameters.FirstOrDefault(p => p.Name == "range");
            Assert.That(rangeParam, Is.Not.Null);
            Assert.That(rangeParam.Required, Is.True);
            Assert.That(rangeParam.ValueType, Is.EqualTo(ParameterValueType.Range));
        }

        [UnitTestAttribute(
            Identifier = "6d2b2553-50cf-44dd-b7af-825744435da5",
            Purpose = "Parameter allowed values are properly set",
            PostCondition = "Allowed values list is populated correctly")]
        [Test]
        public void TestParameterAllowedValues()
        {
            var param = new ContentCreatorParameter("testParam", "Test parameter", ParameterValueType.String)
            {
                AllowedValues = new List<string> { "value1", "value2", "value3" }
            };
            
            Assert.That(param.AllowedValues, Is.Not.Null);
            Assert.That(param.AllowedValues.Count, Is.EqualTo(3));
            Assert.That(param.AllowedValues, Contains.Item("value1"));
            Assert.That(param.AllowedValues, Contains.Item("value2"));
            Assert.That(param.AllowedValues, Contains.Item("value3"));
        }

        [UnitTestAttribute(
            Identifier = "e9adc68f-fe21-4250-9c13-69fae1b644bc",
            Purpose = "RequirementBase content creator includes requirement-specific parameters",
            PostCondition = "Metadata contains RequirementState and RequirementAssignee parameters plus common parameters including ItemProject")]
        [Test]
        public void TestRequirementBaseSpecificParameters()
        {
            var dataSources = Substitute.For<IDataSources>();
            var traceAnalysis = Substitute.For<ITraceabilityAnalysis>();
            var config = Substitute.For<IConfiguration>();
            config.OutputFormat.Returns("ASCIIDOC");
            
            var systemRequirement = new SystemRequirement(dataSources, traceAnalysis, config);
            
            var metadata = systemRequirement.GetMetadata();
            
            Assert.That(metadata, Is.Not.Null);
            Assert.That(metadata.Source, Is.EqualTo("SLMS"));
            Assert.That(metadata.Tags.Count, Is.GreaterThan(0));
            
            var requirementTag = metadata.Tags.FirstOrDefault();
            Assert.That(requirementTag, Is.Not.Null);
            
            // Check that common item parameters are automatically added (should be at the beginning)
            var itemIDParam = requirementTag.Parameters.FirstOrDefault(p => p.Name == "ItemID");
            Assert.That(itemIDParam, Is.Not.Null, "ItemID parameter should be automatically added");
            
            var itemCategoryParam = requirementTag.Parameters.FirstOrDefault(p => p.Name == "ItemCategory");
            Assert.That(itemCategoryParam, Is.Not.Null, "ItemCategory parameter should be automatically added");
            
            var itemStatusParam = requirementTag.Parameters.FirstOrDefault(p => p.Name == "ItemStatus");
            Assert.That(itemStatusParam, Is.Not.Null, "ItemStatus parameter should be automatically added");
            
            var itemTitleParam = requirementTag.Parameters.FirstOrDefault(p => p.Name == "ItemTitle");
            Assert.That(itemTitleParam, Is.Not.Null, "ItemTitle parameter should be automatically added");
            
            var itemProjectParam = requirementTag.Parameters.FirstOrDefault(p => p.Name == "ItemProject");
            Assert.That(itemProjectParam, Is.Not.Null, "ItemProject parameter should be automatically added");
            Assert.That(itemProjectParam.ValueType, Is.EqualTo(ParameterValueType.String));
            Assert.That(itemProjectParam.ExampleValue, Is.EqualTo("ProjectAlpha"));
            
            var sortByParam = requirementTag.Parameters.FirstOrDefault(p => p.Name == "SortBy");
            Assert.That(sortByParam, Is.Not.Null, "SortBy parameter should be automatically added");
            
            // Check for requirement-specific parameters (should be after common parameters)
            var requirementStateParam = requirementTag.Parameters.FirstOrDefault(p => p.Name == "RequirementState");
            Assert.That(requirementStateParam, Is.Not.Null, "RequirementState parameter should be present");
            Assert.That(requirementStateParam.ValueType, Is.EqualTo(ParameterValueType.String));
            Assert.That(requirementStateParam.Required, Is.False);
            Assert.That(requirementStateParam.ExampleValue, Is.EqualTo("Approved"));
            
            var requirementAssigneeParam = requirementTag.Parameters.FirstOrDefault(p => p.Name == "RequirementAssignee");
            Assert.That(requirementAssigneeParam, Is.Not.Null, "RequirementAssignee parameter should be present");
            Assert.That(requirementAssigneeParam.ValueType, Is.EqualTo(ParameterValueType.String));
            Assert.That(requirementAssigneeParam.Required, Is.False);
            Assert.That(requirementAssigneeParam.ExampleValue, Is.EqualTo("John.Doe"));
            
            // Verify that common parameters come before specific parameters
            var itemIDIndex = requirementTag.Parameters.ToList().FindIndex(p => p.Name == "ItemID");
            var reqStateIndex = requirementTag.Parameters.ToList().FindIndex(p => p.Name == "RequirementState");
            Assert.That(itemIDIndex, Is.LessThan(reqStateIndex), "Common parameters should come before requirement-specific parameters");
        }
    }
}
