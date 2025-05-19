using RoboClerk.Configuration;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace RoboClerk
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
        void Initialize(IConfiguration config);
        void ConfigureServices(IServiceCollection services);
    }
}
