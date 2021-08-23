using System.Collections.Generic;

namespace RoboClerk
{
    class Section
    {
        private string title = "";  //title of the section
        private int level = 1; //level of the section with 1 being the highest
        private string text = ""; //contents of the section in raw text
        private List<Section> subsections = new List<Section>();
        private List<Item> items = new List<Item>();

        public Section(string title, int level)
        {
            
        }

        public string Title
        {
            get => title;
            set => title = value;
        }

        public int Level
        {
            get => level;
            set => level = value;
        }

        public string Text
        {
            get => text;            
        }

        public IEnumerable<Item> Items
        {
            get => items;
        }

        public void AddItem(Item item, int position = -1)
        {

        }

        public int CountItems
        {
            get => items.Count;
        }

        public IEnumerable<Section> SubSections
        {
            get => subsections;
        }

        public void AddSubSection(Section section, int position = -1)
        {

        }

        public int CountSubSections
        {
            get => subsections.Count;
        }
    }
}