using Markdig.Renderers.Html;
using Markdig.Renderers.Normalize;
using System.IO;

namespace Markdig.Extensions.RoboClerk
{
    public class NormalizeRoboClerkContainerInlineRenderer : NormalizeObjectRenderer<RoboClerkContainerInline>
    {
        protected override void Write(NormalizeRenderer renderer, RoboClerkContainerInline obj)
        {
            renderer.Write("@@");
            renderer.WriteChildren(obj);
            renderer.Write("@@");
        }
    }
}
