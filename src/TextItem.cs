using System;
using RoboClerk;

namespace RoboClerk
{
    class TextItem : Item
    {
        private string text;
        public TextItem(string txt)
        {
            text = txt;
            type = "TextItem";
            id = Guid.NewGuid().ToString();
        }

        public string Text
        {
            get => text;
        }

        public override string ToMarkDown()
        {
            return text;
        }
    }
}

