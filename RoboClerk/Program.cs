using System;
using System.IO;
using System.Reflection;

namespace RoboClerk
{
    class Program
    {
        static void Main(string[] args)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var projectConfigFile = $"{Path.GetDirectoryName(assembly.Location)}/Configuration/Project/projectConfig.toml";
            var roboClerkConfigFile = $"{Path.GetDirectoryName(assembly.Location)}/Configuration/RoboClerk/RoboClerk.toml";
            
            RoboClerkCore core = new RoboClerkCore(roboClerkConfigFile,projectConfigFile);
            core.GenerateDocs();
            core.SaveMarkdownDocumentsToDisk(DocumentFormat.Markdown);
        }
    }
}
