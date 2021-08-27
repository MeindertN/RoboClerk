using System;
using System.IO;

namespace RoboClerk
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach(var arg in args)
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
            }
        }
    }
}
