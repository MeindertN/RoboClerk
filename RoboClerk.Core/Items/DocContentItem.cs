using System;

namespace RoboClerk
{
    public class DocContentItem : LinkedItem
    {
        public DocContentItem()
        {
            id = Guid.NewGuid().ToString();
            type = "DocContent";
        }

        public string DocContent { get; set; } = string.Empty;
    }
}
