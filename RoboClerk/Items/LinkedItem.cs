using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace RoboClerk
{
    public abstract class LinkedItem : Item
    {
        protected List<ItemLink> linkedItems = new List<ItemLink>();

        [JsonInclude]
        public IEnumerable<ItemLink> LinkedItems
        {
            get
            {
                return linkedItems;
            }
            private set
            {
                linkedItems = (List<ItemLink>)value;
            }
        }

        public void AddLinkedItem(ItemLink itemLink)
        {
            linkedItems.Add(itemLink);
        }

        public ItemLinkType GetItemLinkType(LinkedItem item)
        {
            var result = from s in linkedItems where s.TargetID == item.ItemID select s.LinkType;
            if(result.Count() > 1) //this is a situation that we do not support and that should not occur in practice
            {
                throw new Exception($"The same item with ID \"{item.ItemID}\" was linked multiple times to item with ID \"{id}\".");
            }
            return result.FirstOrDefault(ItemLinkType.None);  
        }
    }
}
