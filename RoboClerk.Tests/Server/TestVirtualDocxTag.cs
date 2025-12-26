using NUnit.Framework;
using NSubstitute;
using RoboClerk.Server.Models;
using RoboClerk.Core.Configuration;
using RoboClerk.Core;

namespace RoboClerk.Tests.Server
{
    [TestFixture]
    [Description("Tests for the VirtualDocxTag that represents virtual content controls")]
    public class TestVirtualDocxTag
    {
        private IConfiguration mockConfiguration;

        [SetUp]
        public void Setup()
        {
            mockConfiguration = Substitute.For<IConfiguration>();
        }

        [UnitTestAttribute(
            Identifier = "F5BE2869-3374-4D3F-88B2-8578A5436FAB",
            Purpose = "VirtualDocxTag is created with numeric content control ID",
            PostCondition = "Tag is created with correct ID")]
        [Test]
        public void CreateVirtualDocxTag_NumericId_Success()
        {
            // Arrange
            string contentControlId = "12345";
            string roboclerkTag = "SLMS:SoftwareRequirement()";

            // Act
            var tag = new VirtualDocxTag(contentControlId, roboclerkTag, mockConfiguration);

            // Assert
            Assert.That(tag, Is.Not.Null);
            Assert.That(tag.ContentControlId, Is.EqualTo(contentControlId));
        }

        [UnitTestAttribute(
            Identifier = "A57D2F48-6698-4086-B0E4-CA3F5071B999",
            Purpose = "VirtualDocxTag is created with non-numeric content control ID",
            PostCondition = "Tag is created with ID converted to hash")]
        [Test]
        public void CreateVirtualDocxTag_NonNumericId_Success()
        {
            // Arrange
            string contentControlId = "my-content-control-guid";
            string roboclerkTag = "SLMS:SoftwareRequirement()";

            // Act
            var tag = new VirtualDocxTag(contentControlId, roboclerkTag, mockConfiguration);

            // Assert
            Assert.That(tag, Is.Not.Null);
            Assert.That(tag.ContentControlId, Is.EqualTo(contentControlId));
        }

        [UnitTestAttribute(
            Identifier = "37F8BC0F-F033-42ED-B0CF-C46438F7A486",
            Purpose = "VirtualDocxTag is created with null configuration",
            PostCondition = "Tag is created without throwing exception")]
        [Test]
        public void CreateVirtualDocxTag_NullConfiguration_Success()
        {
            // Arrange
            string contentControlId = "cc1";
            string roboclerkTag = "SLMS:SoftwareRequirement()";

            // Act
            var tag = new VirtualDocxTag(contentControlId, roboclerkTag, null);

            // Assert
            Assert.That(tag, Is.Not.Null);
        }

        [UnitTestAttribute(
            Identifier = "F7DD8774-ECA0-4BE6-A737-61618237367F",
            Purpose = "VirtualDocxTag parses SLMS tag correctly",
            PostCondition = "Source is set to SLMS")]
        [Test]
        public void CreateVirtualDocxTag_SLMSTag_ParsesCorrectly()
        {
            // Arrange
            string contentControlId = "cc1";
            string roboclerkTag = "SLMS:SoftwareRequirement()";

            // Act
            var tag = new VirtualDocxTag(contentControlId, roboclerkTag, mockConfiguration);

            // Assert
            Assert.That(tag.Source, Is.EqualTo(DataSource.SLMS));
        }

        [UnitTestAttribute(
            Identifier = "D750809C-F430-43AE-BF7A-F8B95EAD36AD",
            Purpose = "VirtualDocxTag parses tag with parameters correctly",
            PostCondition = "Parameters are extracted from tag")]
        [Test]
        public void CreateVirtualDocxTag_TagWithParameters_ParsesParameters()
        {
            // Arrange
            string contentControlId = "cc1";
            string roboclerkTag = "SLMS:SoftwareRequirement(ID=SWR001)";

            // Act
            var tag = new VirtualDocxTag(contentControlId, roboclerkTag, mockConfiguration);

            // Assert
            Assert.That(tag.HasParameter("ID"), Is.True);
            Assert.That(tag.GetParameterOrDefault("ID", ""), Is.EqualTo("SWR001"));
        }

        [UnitTestAttribute(
            Identifier = "B1011D28-0310-45AB-9795-049FDEEECB2C",
            Purpose = "VirtualDocxTag inherits from RoboClerkDocxTag",
            PostCondition = "Tag can be used polymorphically")]
        [Test]
        public void VirtualDocxTag_InheritsFromRoboClerkDocxTag()
        {
            // Arrange
            string contentControlId = "cc1";
            string roboclerkTag = "SLMS:SoftwareRequirement()";

            // Act
            var tag = new VirtualDocxTag(contentControlId, roboclerkTag, mockConfiguration);

            // Assert
            Assert.That(tag, Is.InstanceOf<RoboClerk.Core.DocxSupport.RoboClerkDocxTag>());
        }

        [UnitTestAttribute(
            Identifier = "9CDFB228-4C7C-42F5-87F0-3F7CB1E5FE4B",
            Purpose = "VirtualDocxTag ContentControlId property returns virtual ID",
            PostCondition = "Virtual content control ID is returned")]
        [Test]
        public void ContentControlId_ReturnsVirtualId()
        {
            // Arrange
            string contentControlId = "virtual-cc-123";
            string roboclerkTag = "SLMS:SoftwareRequirement()";

            // Act
            var tag = new VirtualDocxTag(contentControlId, roboclerkTag, mockConfiguration);

            // Assert
            Assert.That(tag.ContentControlId, Is.EqualTo("virtual-cc-123"));
        }

        [UnitTestAttribute(
            Identifier = "6A123E14-6A68-4476-9291-EB3369F8A62E",
            Purpose = "VirtualDocxTag contents can be set and retrieved",
            PostCondition = "Contents property works correctly")]
        [Test]
        public void VirtualDocxTag_ContentsProperty_Works()
        {
            // Arrange
            string contentControlId = "cc1";
            string roboclerkTag = "SLMS:SoftwareRequirement()";
            var tag = new VirtualDocxTag(contentControlId, roboclerkTag, mockConfiguration);

            // Act
            tag.Contents = "Test content";

            // Assert
            Assert.That(tag.Contents, Is.EqualTo("Test content"));
        }

        [UnitTestAttribute(
            Identifier = "2592AE60-8928-4116-80AD-29DE6C172ED4",
            Purpose = "VirtualDocxTag parses Trace tag correctly",
            PostCondition = "Source is set to Trace")]
        [Test]
        public void CreateVirtualDocxTag_TraceTag_ParsesCorrectly()
        {
            // Arrange
            string contentControlId = "cc1";
            string roboclerkTag = "Trace:SWR(ID=SWR001)";

            // Act
            var tag = new VirtualDocxTag(contentControlId, roboclerkTag, mockConfiguration);

            // Assert
            Assert.That(tag.Source, Is.EqualTo(DataSource.Trace));
        }

        [UnitTestAttribute(
            Identifier = "077F1579-E15A-4D8D-B6DA-980BE314D8B8",
            Purpose = "VirtualDocxTag parses Config tag correctly",
            PostCondition = "Source is set to Config")]
        [Test]
        public void CreateVirtualDocxTag_ConfigTag_ParsesCorrectly()
        {
            // Arrange
            string contentControlId = "cc1";
            string roboclerkTag = "Config:ProjectName()";

            // Act
            var tag = new VirtualDocxTag(contentControlId, roboclerkTag, mockConfiguration);

            // Assert
            Assert.That(tag.Source, Is.EqualTo(DataSource.Config));
        }

        [UnitTestAttribute(
            Identifier = "5F89D72B-E793-40A1-A7D4-44F852FA813D",
            Purpose = "VirtualDocxTag parses Source tag correctly",
            PostCondition = "Source is set to Source")]
        [Test]
        public void CreateVirtualDocxTag_SourceTag_ParsesCorrectly()
        {
            // Arrange
            string contentControlId = "cc1";
            string roboclerkTag = "Source:UnitTests()";

            // Act
            var tag = new VirtualDocxTag(contentControlId, roboclerkTag, mockConfiguration);

            // Assert
            Assert.That(tag.Source, Is.EqualTo(DataSource.Source));
        }

        [UnitTestAttribute(
            Identifier = "4D396EED-CAC9-4060-B4C8-3DAD7D33D1BB",
            Purpose = "VirtualDocxTag ContentCreatorID is parsed correctly",
            PostCondition = "ContentCreatorID property has expected value")]
        [Test]
        public void CreateVirtualDocxTag_ContentCreatorID_ParsedCorrectly()
        {
            // Arrange
            string contentControlId = "cc1";
            string roboclerkTag = "SLMS:SoftwareRequirement()";

            // Act
            var tag = new VirtualDocxTag(contentControlId, roboclerkTag, mockConfiguration);

            // Assert
            Assert.That(tag.ContentCreatorID, Is.EqualTo("SoftwareRequirement"));
        }
    }
}
