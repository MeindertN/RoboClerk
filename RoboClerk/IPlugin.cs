using RoboClerk.Configuration;

namespace RoboClerk
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
        void Initialize(IConfiguration config);
    }
}
