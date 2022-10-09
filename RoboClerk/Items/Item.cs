using System;

namespace RoboClerk
{
    public abstract class Item
    {
        protected string id = string.Empty;
        protected string type = string.Empty;
        protected string category = string.Empty;
        protected string revision = string.Empty;
        protected DateTime lastUpdated = DateTime.MinValue;
        protected Uri link = null;

        public string ItemID
        {
            get => id;
            set => id = value;
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