
namespace RoboClerk.Core.Configuration
{
    public class TruthItemConfig
    {
        public string Name { get; }
        public bool Filtered { get; }

        public TruthItemConfig()
        {
            Name = string.Empty;
            Filtered = false;
        }

        public TruthItemConfig(string name, bool filter)
        {
            Name = name;
            Filtered = filter;
        }
    }
}
