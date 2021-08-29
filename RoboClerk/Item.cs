using System;

namespace RoboClerk
{
    public abstract class Item
    {
        protected string id = "";
        protected string type = "";

        public abstract string ToMarkDown();
        
        public string ItemID 
        {
            get => id;
            set => id = value;
        }   

        public string ItemType
        {
            get => type;
        } 
    }
}