namespace RoboClerk
{
    public class ExternalDependency
    {
        private string name;
        private string version;
        private bool conflict;

        public ExternalDependency(string name, string version, bool conflict)
        {
            this.name = name;
            this.version = version;
            this.conflict = conflict;
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string Version
        {
            get { return version; }
            set { version = value; }
        }

        public bool Conflict
        {
            get { return conflict; }
            set { conflict = value; }
        }
    }
}
