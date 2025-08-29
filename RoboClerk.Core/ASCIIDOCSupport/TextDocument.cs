using RoboClerk.Core;
using System.Text;
using System.Text.RegularExpressions;

namespace RoboClerk
{
    public class TextDocument : IDocument
    {
        protected string title = string.Empty;
        protected string rawText = string.Empty;
        protected string templateFile = string.Empty;

        protected List<RoboClerkTextTag> roboclerkTags = new List<RoboClerkTextTag>();

        public TextDocument(string title, string templateFile)
        {
            this.title = title;
            this.templateFile = templateFile;
        }

        public void FromString(string text)
        {
            //normalize the line endings in the string
            rawText = Regex.Replace(text, @"\r\n", "\n");

            try
            {
                roboclerkTags.Clear();
                roboclerkTags = RoboClerkTextParser.ExtractRoboClerkTags(rawText);
            }
            catch (TagInvalidException e)
            {
                e.DocumentTitle = title;
                throw;
            }
        }

        public void FromStream(Stream textStream)
        {
            var sr = new StreamReader(textStream, Encoding.UTF8);
            var fileText = sr.ReadToEnd();
            FromString(fileText);
        }

        public string ToText()
        {
            //this function can be called at any time, remove the tags and relace them with 
            //the tag contents that could have been updated since the document was parsed. 
            //The document can be updated by replacing the individual tag contents.
            return RoboClerkTextParser.ReInsertRoboClerkTags(rawText, roboclerkTags);
        }

        public void SaveToFile(string filePath)
        {
            File.WriteAllText(filePath, ToText());
        }

        public DocumentType DocumentType => DocumentType.Text;

        // IDocument interface implementation
        IEnumerable<IRoboClerkTag> IDocument.RoboClerkTags => roboclerkTags.Cast<IRoboClerkTag>();

        public string Title
        {
            get => title;
            set => title = value;
        }

        public string TemplateFile
        {
            get => templateFile;
        }
    }
}