// Part of this file Copyright (c) Alexandre Mutel. All rights reserved.
// Those parts are licensed under the BSD-Clause 2 license. 
// See the license file in the project root for more information.

using Markdig.Parsers;
using Markdig.Helpers;
using Markdig.Syntax;

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

        /*private int CountAndSkipChar(char matchChar, StringSlice slice)
        {
            string text = slice.Text;
            int end = slice.End;
            int current = slice.Start;

            while (current <= end && (uint)current < (uint)text.Length && text[current] == matchChar)
            {
                current++;
            }

            int count = current - slice.Start;
            slice.Start = current;
            return count;
        }

        public override BlockState TryContinue(BlockProcessor processor, Block block)
        {
            var fence = (IFencedBlock)block;
            var openingCount = fence.OpeningFencedCharCount;

            // Match if we have a closing fence
            var line = processor.Line;
            var sourcePosition = processor.Start;
            var closingCount = CountAndSkipChar(fence.FencedChar,line);
            var diff = openingCount - closingCount;

            char c = line.CurrentChar;
            var lastFenceCharPosition = processor.Start + closingCount;

            // If we have a closing fence, close it and discard the current line
            // The line must contain only fence opening character followed only by whitespaces.
            var startBeforeTrim = line.Start;
            var endBeforeTrim = line.End;
            var trimmed = line.TrimEnd();
            if (diff <= 0 && !processor.IsCodeIndent && (c == '\0' || c.IsWhitespace()) && trimmed)
            {
                block.UpdateSpanEnd(startBeforeTrim - 1);

                var fencedBlock = (IFencedBlock)block;
                fencedBlock.ClosingFencedCharCount = closingCount;
                fencedBlock.NewLine = processor.Line.NewLine;
                fencedBlock.TriviaBeforeClosingFence = processor.UseTrivia(sourcePosition - 1);
                fencedBlock.TriviaAfter = new StringSlice(processor.Line.Text, lastFenceCharPosition, endBeforeTrim);

                // Don't keep the last line
                return BlockState.BreakDiscard;
            }

            // Reset the indentation to the column before the indent
            processor.GoToColumn(processor.ColumnBeforeIndent);

            return BlockState.ContinueDiscard; //we don't parse what is in the RoboClerk block, discard the line
        }*/
    }
}