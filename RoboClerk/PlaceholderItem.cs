using System;
using System.Text;
using RoboClerk;

namespace RoboClerk
{
    class PlaceholderItem : Item
    {
        private string stringContent;
        
        public PlaceholderItem()
        {
            type = "PlaceholderItem";
            id = Guid.NewGuid().ToString();
        }
        public override string ToMarkDown()
        {
            return stringContent;
        }

        public string StringContent 
        {
            get => stringContent;
            set => stringContent = value;
        }
    }
}

