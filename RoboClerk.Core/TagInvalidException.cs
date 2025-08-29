using System;
using System.Linq;

namespace RoboClerk.Core
{
    public class TagInvalidException : Exception
    {
        private int line = -1;
        private int character = -1;
        private string tagContents = string.Empty;
        private string documentTitle = string.Empty;
        private string reason = string.Empty;

        public TagInvalidException(string tagContents, string reason)
        {
            this.tagContents = tagContents;
            this.reason = reason;
        }

        public string TagContents
        {
            get => tagContents;
        }

        public string Reason
        {
            get => reason;
        }

        public string DocumentTitle
        {
            set => documentTitle = value;
        }

        public override string Message
        {
            get
            {
                if (documentTitle == string.Empty)
                {
                    if (line == -1 || character == -1)
                    {
                        return $"{reason}. Tag contents: {tagContents}";
                    }
                    else
                    {
                        return $"{reason} at ({line}:{character}). Tag contents: {tagContents}";
                    }
                }
                else
                {
                    if (line == -1 || character == -1)
                    {
                        return $"{reason} in {documentTitle} template. Tag contents: {tagContents}";
                    }
                    else
                    {
                        return $"{reason} in {documentTitle} template at ({line}:{character}). Tag contents: {tagContents}";
                    }
                }
            }
        }

        public void SetLocation(int tagStart, string doc)
        {
            string relevantPart = doc.Substring(0, tagStart);
            line = relevantPart.Count(f => (f == '\n')) + 1;
            character = tagStart - relevantPart.LastIndexOf('\n');
        }
    }
}