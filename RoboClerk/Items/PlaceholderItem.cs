using System;

namespace RoboClerk
{
    public class PlaceholderItem : Item
    {
        private string stringContent;

        public PlaceholderItem()
        {
            type = "PlaceholderItem";
            id = Guid.NewGuid().ToString();
        }

        public string StringContent
        {
            get => stringContent;
            set => stringContent = value;
        }
    }
}

