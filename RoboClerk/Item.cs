using System;

namespace RoboClerk
{
    abstract class Item
    {
        protected string id = "";
        protected string type = "";
        public abstract string ToMarkDown();
        
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