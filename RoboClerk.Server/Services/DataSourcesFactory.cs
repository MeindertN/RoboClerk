using RoboClerk.Core;
using IConfiguration = RoboClerk.Core.Configuration.IConfiguration;

namespace RoboClerk.Server.Services
{
    public class DataSourcesFactory : IDataSourcesFactory
    {
        private readonly IServiceProvider serviceProvider;
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public DataSourcesFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IDataSources CreateDataSources(IConfiguration configuration)
        {
            try
            {
                if (configuration.CheckpointConfig.CheckpointFile == string.Empty)
                {
                    // Use plugin data sources
                    var pluginLoader = serviceProvider.GetRequiredService<IPluginLoader>();
                    var fileProvider = serviceProvider.GetRequiredService<IFileProviderPlugin>();
                    return new PluginDataSources(configuration, pluginLoader, fileProvider);
                }
                else
                {
                    // Use checkpoint data sources
                    var pluginLoader = serviceProvider.GetRequiredService<IPluginLoader>();
                    var fileProvider = serviceProvider.GetRequiredService<IFileProviderPlugin>();
                    return new CheckpointDataSources(configuration, pluginLoader, fileProvider, configuration.CheckpointConfig.CheckpointFile);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to create data sources");
                throw;
            }
        }
    }
}