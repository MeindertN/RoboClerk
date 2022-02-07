namespace RoboClerk
{
    public class TraceEntity
    {
        private readonly string name = string.Empty;
        private readonly string abbreviation = string.Empty;
        private readonly string id = string.Empty;

        public TraceEntity(string id, string name, string abbreviation)
        {
            this.name = name;
            this.abbreviation = abbreviation;
            this.id = id;
        }

        public string Name
        {
            get => name;
        }

        public string Abbreviation
        {
            get => abbreviation;
        }

        public string ID
        {
            get => id;
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            TraceEntity comp = obj as TraceEntity;
            if (comp == null)
            {
                return false;
            }
            return comp.ID == id;
        }
    }
}
