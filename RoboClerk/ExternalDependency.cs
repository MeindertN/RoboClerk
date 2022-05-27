namespace RoboClerk
{
    public class ExternalDependency
    {
        private string name;
        private string version;

        ExternalDependency(string name, string version)
        {
            this.name = name;
            this.version = version;
        }

        public string Name { get { return name; } }
        public string Version { get { return version; } }
    }
}
