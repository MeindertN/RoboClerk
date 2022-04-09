namespace RoboClerk
{
    public enum TraceEntityType
    {
        Truth,
        Document,
        Unknown
    };

    public class TraceEntity
    {
        private readonly string name = string.Empty;
        private readonly string abbreviation = string.Empty;
        private readonly string id = string.Empty;
        private readonly TraceEntityType type = TraceEntityType.Unknown;

        public TraceEntity(string id, string name, string abbreviation, TraceEntityType tp)
        {
            this.name = name;
            this.abbreviation = abbreviation;
            this.id = id;
            this.type = tp;
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

        public TraceEntityType EntityType
        {
            get => type;
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
