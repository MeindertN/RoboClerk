using Markdig.Renderers.Html;
using Markdig.Renderers.Normalize;
using System.IO;

namespace Markdig.Extensions.RoboClerk
{
    public class NormalizeRoboClerkContainerRenderer : NormalizeObjectRenderer<RoboClerkContainer>
    {
        protected override void Write(NormalizeRenderer renderer, RoboClerkContainer obj)
        {
            renderer.Write("@@@");
            if (obj.Info != null)
            {
                renderer.Write(obj.Info);
            }
            renderer.WriteLine();
            renderer.WriteChildren(obj);
            renderer.EnsureLine();
            renderer.Write("@@@");
            renderer.FinishBlock(true);
        }

        
    }
}
