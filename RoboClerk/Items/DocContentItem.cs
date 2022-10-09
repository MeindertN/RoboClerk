using System;

namespace RoboClerk
{
    public class DocContentItem : LinkedItem
    {
        public DocContentItem()
        {
            id = Guid.NewGuid().ToString();
        }

        public string Contents { get; set; }
    }
}
