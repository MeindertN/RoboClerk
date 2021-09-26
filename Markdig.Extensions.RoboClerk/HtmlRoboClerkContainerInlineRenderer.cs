// Part of this file Copyright (c) Alexandre Mutel. All rights reserved.
// Those parts are licensed under the BSD-Clause 2 license. 
// See the license file in the project root for more information.

using Markdig.Renderers;
using Markdig.Renderers.Html;
using System.IO;

namespace Markdig.Extensions.RoboClerk
{
    public class HtmlRoboClerkContainerInlineRenderer : HtmlObjectRenderer<RoboClerkContainerInline>
    {
        protected override void Write(HtmlRenderer renderer, RoboClerkContainerInline obj)
        {
            renderer.Write("<span").WriteAttributes(obj).Write('>');
            var stringValue = obj.FirstChild.ToString();
            renderer.Write(stringValue.Substring(0,stringValue.IndexOf('(')));
            renderer.Write("</span>");
        }
    }
}