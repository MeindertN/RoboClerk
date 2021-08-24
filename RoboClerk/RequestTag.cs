using Markdig;
using Markdig.Syntax;
using Markdig.Extensions.CustomContainers;

namespace RoboClerk
{
    public enum DataSource
    {
        SLMS, //the software lifecycle management system
        Source, //found in the source code
        Config, //found in the RoboClerk project configuration file
        OTS, //found in a binary control system
        Unknown //it is not known where to retrieve this information
    }

    class RequestTag
    {
        private int start = -1;
        private int end = -1;
        private string info = "";
        private DataSource source = DataSource.Unknown;
        public RequestTag(CustomContainer tag, string rawDocument)
        {
            (info,source) = ParseInfo(tag.Info, tag.Span, rawDocument);
        }

        public RequestTag(CustomContainerInline tag)
        {
            
        }

        private (string,DataSource) ParseInfo(string tagInfo, SourceSpan span, string rawDocument)
        {
            //parse the tagInfo, items are separated by :
            var items = tagInfo.Split(':');
            if(items.Length != 2)
            {
                throw new System.Exception($"Error parsing CustomContainer tag: {tagInfo}. Two elements separated by : expected but not found.");
            }
            DataSource src = DataSource.Unknown;
            switch(items[1]) 
            {
                case "SLMS": src = DataSource.SLMS; break;
                case "Source": src = DataSource.Source; break;
                case "Config": src = DataSource.Config; break;
                case "OTS": src = DataSource.OTS; break;
            }
            return (items[0],src);
        }
    }
}