using System;

namespace RoboClerk
{
    public abstract class Item
    {
        protected string id = string.Empty;
        protected string title = string.Empty;
        //Note that the type must match the name of the truth item as defined in the projectConfig.
        //E.g. the type of the software requirement item is "SoftwareRequirement"
        protected string type = string.Empty;  
        protected string category = string.Empty;
        protected string revision = string.Empty;
        protected string targetVersion = string.Empty;
        protected DateTime lastUpdated = DateTime.MinValue;
        protected Uri link = null;

        public string ItemID
        {
            get => id;
            set => id = value;
        }

        public string ItemTitle
        {
            get => title;
            set => title = value;
        }

        public string ItemCategory
        {
            get => category;
            set => category = value;
        }

        public string ItemType
        {
            get => type;
        }

        public string ItemRevision
        {
            set => revision = value;
            get => revision;
        }

        public string ItemTargetVersion
        {
            set => targetVersion = value;
            get => targetVersion;
        }

        public DateTime ItemLastUpdated
        {
            get => lastUpdated;
            set => lastUpdated = value;
        }

        public bool HasLink
        {
            get => link != null;
        }

        public Uri Link
        {
            get => link;
            set => link = value; 
        }
    }
}