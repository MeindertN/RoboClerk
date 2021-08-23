using System;

namespace RoboClerk
{
    abstract class Item
    {
        protected string id = "";
        protected string type = "";
        public abstract string ToMarkDown();
        public abstract void FromMarkDown();
        
        public string ID 
        {
            get => id;
            set => id = value;
        }   

        public string Type
        {
            get => type;
        } 
    }
}