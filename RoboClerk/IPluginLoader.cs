namespace RoboClerk
{
    public interface IPluginLoader
    {
        public T LoadPlugin<T>(string name, string pluginDir) where T : class;
    }
}