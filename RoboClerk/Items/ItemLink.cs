using System;

namespace RoboClerk
{
    public enum ItemLinkType
    {
        Parent,
        Child,
        Related,
        TestedBy,
        Tests,
        Predecessor,
        Successor,
        Duplicate,
        Affects,
        AffectedBy,
        DOC,   //a special link type for linking to a document
        None
    };

    public class ItemLink
    {
        private string targetID = string.Empty;
        private ItemLinkType linkType = ItemLinkType.None;

        public ItemLink(string targetID, ItemLinkType linkType)
        {
            this.targetID = targetID;
            this.linkType = linkType;
        }

        public string TargetID { get { return targetID; } }
        public ItemLinkType LinkType { get { return linkType; } }

        public static ItemLinkType GetLinkTypeForString(string lt)
        {
            if (string.IsNullOrEmpty(lt))
            {
                throw new ArgumentException("Unable to convert empty or null string to ItemLinkType.");
            }
            try
            {
                return (ItemLinkType)Enum.Parse(typeof(ItemLinkType), lt, true);
            }
            catch
            {
                throw new Exception($"Link type \"{lt}\" is unknown, check your project configuration file.");
            }
        }
    }
}
