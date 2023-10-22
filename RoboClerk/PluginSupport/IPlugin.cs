using RoboClerk.Configuration;
using System.Collections.Generic;

namespace RoboClerk
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
        void Initialize(IConfiguration config);
    }
}
