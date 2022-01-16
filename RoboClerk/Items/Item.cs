using System;

namespace RoboClerk
{
    public abstract class Item
    {
        protected string id = "";
        protected string type = "";
        protected Uri link = null;

        public abstract string ToText();
        
        public string ItemID 
        {
            get => id;
            set => id = value;
        }   

        public string ItemType
        {
            get => type;
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