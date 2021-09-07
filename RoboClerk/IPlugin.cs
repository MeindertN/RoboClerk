using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
        void Initialize();
    }
}
