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
            core.SaveDocumentsToDisk();

/*            foreach (var arg in args)
            {
                var mdFile = File.ReadAllText(args[0]);
                SoftwareRequirementsSpecification srs = new SoftwareRequirementsSpecification();
                srs.FromMarkDown(mdFile);
                int index = 0;
                foreach(var tag in srs.RoboClerkTags)
                {
                    tag.Contents = $"Index{index}";
                    index++;
                }
                string output = srs.ToMarkDown();
                Console.Write(output);
            }*/
        }
    }
}
