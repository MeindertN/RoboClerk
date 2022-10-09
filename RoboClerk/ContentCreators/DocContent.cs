using RoboClerk.Configuration;
using System;
using System.Text;

namespace RoboClerk.ContentCreators
{
    internal class DocContent : ContentCreatorBase
    {
        public override string GetContent(RoboClerkTag tag, IDataSources data, ITraceabilityAnalysis analysis, DocumentConfig doc)
        {
            var te = analysis.GetTraceEntityForID("DocContent");
            if (te == null)
            {
                throw new Exception("DocContent trace entity is missing, this trace entity must be present for RoboClerk to function.");
            }
            bool foundContent = false;
            var docContents = data.GetAllDocContents();
            //No selection needed, we return everything
            StringBuilder output = new StringBuilder();
            var properties = typeof(DocContentItem).GetProperties();
            foreach (var content in docContents)
            {
                if (ShouldBeIncluded(tag, content, properties) && CheckUpdateDateTime(tag, content))
                {
                    foundContent = true;
                    try
                    {
                        output.AppendLine(content.Contents);
                    }
                    catch
                    {
                        logger.Error($"An error occurred while rendering docContent {content.ItemID} in {doc.DocumentTitle}.");
                        throw;
                    }
                    analysis.AddTrace(te, content.ItemID, analysis.GetTraceEntityForTitle(doc.DocumentTitle), content.ItemID);
                }
            }
            if (!foundContent)
            {
                return $"WARNING: Unable to find DocContent(s). Check if DocContents of the correct type are provided or if a valid DocContent identifier is specified.";
            }
            return output.ToString();
        }
    }
}
