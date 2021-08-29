
namespace RoboClerk
{
    public class RoboClerkCore
    {
        ISLMSPlugin SLMSPlugin = null;
        public RoboClerkCore()
        {
            LoadPlugins();
            SLMSPlugin.Initialize("meindert", "RoboClerk", "");
            SLMSPlugin.RefreshItems();
        }

        private void LoadPlugins()
        {
            foreach(var plugin in PluginLoader.LoadPlugins(@"RoboClerk.AzureDevOps\bin\Debug\netcoreapp3.1\RoboClerk.AzureDevOps.dll"))
            {
                if(plugin.Name == "AzureDevOpsSLMSPlugin")
                {
                    SLMSPlugin = plugin;
                    break;
                }
            }
        }

        public void GenerateDocs()
        {
            //load the template from into a document structure
            //go over the tag list to determine what information should be collected from where
            //for each tag, request the information items from the appropriate plugin interface
            //convert the items to markdown and associate the markdown with the tags
        }
    }
}
