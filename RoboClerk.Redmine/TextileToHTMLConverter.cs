using Textile;

namespace RoboClerk.Redmine
{
    internal class TextileToHTMLConverter : ITextileConverter
    {
        public string Convert(string text)
        {
            return TextileFormatter.FormatString(text);
        }
    }
}
