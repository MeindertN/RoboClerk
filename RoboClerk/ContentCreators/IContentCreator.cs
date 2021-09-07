using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk.ContentCreators
{
    public interface IContentCreator
    {
        public string GetContent(DataSources data);
    }
}
