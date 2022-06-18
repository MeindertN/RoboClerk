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

        public string Name { get { return name; } }
        public string Version { get { return version; } }
        public bool Conflict { get { return conflict; } }
    }
}
