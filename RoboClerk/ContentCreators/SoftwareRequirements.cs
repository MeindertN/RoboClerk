using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class SoftwareRequirements : IContentCreator
    {
        public SoftwareRequirements()
        {
            
        }

        public string GetContent(DataSources sources)
        {
            var requirements = sources.GetAllSoftwareRequirements();
            //No selection needed, we return everything
            StringBuilder output = new StringBuilder();
            foreach(var requirement in requirements)
            {
                output.AppendLine(requirement.ToMarkDown());
            }
            return output.ToString();
        }
    }
}
