using System.IO.Abstractions;

namespace RoboClerk
{
    public interface IPluginLoader
    {
        public T LoadPlugin<T>(string name, string pluginDir, IFileSystem fileSystem) where T : class;
    }
}