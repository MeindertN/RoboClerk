using RoboClerk.Core.Configuration;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace RoboClerk
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
        void InitializePlugin(IConfiguration config);
        void ConfigureServices(IServiceCollection services);
    }
}
