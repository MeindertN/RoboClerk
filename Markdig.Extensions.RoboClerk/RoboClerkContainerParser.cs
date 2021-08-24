// Part of this file Copyright (c) Alexandre Mutel. All rights reserved.
// Those parts are licensed under the BSD-Clause 2 license. 
// See the license file in the project root for more information.

using Markdig.Parsers;

namespace Markdig.Extensions.RoboClerk
{
    public class RoboClerkContainerParser : FencedBlockParserBase<RoboClerkContainer>
    {
        public RoboClerkContainerParser()
        {
            OpeningCharacters = new [] {'@'};

            // We don't need a prefix
            InfoPrefix = null;
        }

        protected override RoboClerkContainer CreateFencedBlock(BlockProcessor processor)
        {
            return new RoboClerkContainer(this);
        }
    }
}