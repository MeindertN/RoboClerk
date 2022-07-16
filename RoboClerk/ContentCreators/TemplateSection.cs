using RoboClerk.Configuration;

namespace RoboClerk.ContentCreators
{
    public class TemplateSection : ContentCreatorBase
    {
        public override string GetContent(RoboClerkTag tag, IDataSources data, ITraceabilityAnalysis analysis, DocumentConfig doc)
        {
            string filename = tag.GetParameterOrDefault("FILENAME",string.Empty);
            if(filename == string.Empty)
            {
                throw new TagInvalidException(tag.Contents, $"TemplateSection tag without valid fileName parameter found in {doc.DocumentTitle}");
            }
            try
            {
                return data.GetTemplateFile(filename);
            }
            catch
            {
                logger.Error($"Error occurred trying to load \"{filename}\" from the template directory. Ensure \"{filename}\" is in the input directory.");
                throw;
            }
        }
    }
}
