// Part of this file Copyright (c) Alexandre Mutel. All rights reserved.
// Those parts are licensed under the BSD-Clause 2 license. 
// See the license file in the project root for more information.

using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Normalize;

namespace Markdig.Extensions.RoboClerk
{
    public class RoboClerkContainerExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.BlockParsers.Contains<RoboClerkContainerParser>())
            {
                // Insert the parser before any other parsers
                pipeline.BlockParsers.Insert(0, new RoboClerkContainerParser());
            }

            // Plug the inline parser for RoboClerkContainerInline
            var inlineParser = pipeline.InlineParsers.Find<EmphasisInlineParser>();
            if (inlineParser != null && !inlineParser.HasEmphasisChar('@'))
            {
                inlineParser.EmphasisDescriptors.Add(new EmphasisDescriptor('@', 2, 2, true));
                inlineParser.TryCreateEmphasisInlineList.Add((emphasisChar, delimiterCount) =>
                {
                    if (delimiterCount == 2 && emphasisChar == '@')
                    {
                        return new RoboClerkContainerInline();
                    }
                    return null;
                });
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                if (!htmlRenderer.ObjectRenderers.Contains<HtmlRoboClerkContainerRenderer>())
                {
                    // Must be inserted before CodeBlockRenderer
                    htmlRenderer.ObjectRenderers.Insert(0, new HtmlRoboClerkContainerRenderer());
                }
                if (!htmlRenderer.ObjectRenderers.Contains<HtmlRoboClerkContainerInlineRenderer>())
                {
                    // Must be inserted before EmphasisRenderer
                    htmlRenderer.ObjectRenderers.Insert(0, new HtmlRoboClerkContainerInlineRenderer());
                }
            }
            if (renderer is NormalizeRenderer normalizeRenderer)
            {
                if (!normalizeRenderer.ObjectRenderers.Contains<NormalizeRoboClerkContainerRenderer>())
                {
                    normalizeRenderer.ObjectRenderers.Insert(0, new NormalizeRoboClerkContainerRenderer());
                }
                if (!normalizeRenderer.ObjectRenderers.Contains<NormalizeRoboClerkContainerInlineRenderer>())
                {
                    normalizeRenderer.ObjectRenderers.Insert(0, new NormalizeRoboClerkContainerInlineRenderer());
                }
            }
        }
    }
}