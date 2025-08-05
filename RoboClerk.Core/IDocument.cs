namespace RoboClerk.Core
{
    internal interface IDocument
    {
        void FromStream(Stream stream);
        IEnumerable<IRoboClerkTag> RoboClerkTags { get; }
        string Title { get; set; }
        string TemplateFile { get; }
    }
}
