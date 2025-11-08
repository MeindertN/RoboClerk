using RoboClerk.Core;
using IConfiguration = RoboClerk.Core.Configuration.IConfiguration;

namespace RoboClerk.Server.Services
{
    public interface IDataSourcesFactory
    {
        IDataSources CreateDataSources(IConfiguration configuration);
    }
}