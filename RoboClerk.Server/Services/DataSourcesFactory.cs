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
                var pluginLoader = serviceProvider.GetRequiredService<IPluginLoader>();
                var fileProvider = serviceProvider.GetRequiredService<IFileProviderPlugin>();
                
                if (configuration.CheckpointConfig.CheckpointFile == string.Empty)
                {
                    // Use plugin data sources with the smart file provider
                    // The smart provider handles routing to local or other file provider automatically
                    logger.Info("Creating plugin data sources with smart file provider routing");
                    return new PluginDataSources(configuration, pluginLoader, fileProvider);
                }
                else
                {
                    // Use checkpoint data sources
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