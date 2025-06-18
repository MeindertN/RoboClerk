﻿using System;

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
        RiskControl, //links from a risk to a risk control
        Risk, //links from a risk control to a risk
        DOC,   //a special link type for linking to a document
        UnitTest, //special link type for linking to a unit test
        None
    };

    public class ItemLink
    {
        private string targetID = string.Empty;
        private string targetType = string.Empty;
        private ItemLinkType linkType = ItemLinkType.None;


        public ItemLink(string targetID, ItemLinkType linkType)
        {
            this.targetID = targetID;
            this.linkType = linkType;
        }

        public string TargetID { get { return targetID; } }
        public ItemLinkType LinkType
        {
            set { linkType = value; }
            get { return linkType; }
        }

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
