using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public class ProductRequirements : IContentCreator
    {
        public ProductRequirements()
        {

        }

        public string GetContent(DataSources sources)
        {
            var requirements = sources.GetAllProductRequirements();
            //No selection needed, we return everything
            StringBuilder output = new StringBuilder();
            foreach (var requirement in requirements)
            {
                output.AppendLine(requirement.ToMarkDown());
            }
            return output.ToString();
        }
    }
}
