// Part of this file Copyright (c) Alexandre Mutel. All rights reserved.
// Those parts are licensed under the BSD-Clause 2 license. 
// See the license file in the project root for more information.

using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Markdig.Extensions.RoboClerk
{
    public class HtmlRoboClerkContainerRenderer : HtmlObjectRenderer<RoboClerkContainer>
    {
        protected override void Write(HtmlRenderer renderer, RoboClerkContainer obj)
        {
            renderer.EnsureLine();
            if (renderer.EnableHtmlForBlock)
            {
                renderer.Write("<div").WriteAttributes(obj).Write('>');
            }
            // We don't escape a RoboClerkContainer
            renderer.WriteChildren(obj);
            if (renderer.EnableHtmlForBlock)
            {
                renderer.WriteLine("</div>");
            }
        }
    }
}