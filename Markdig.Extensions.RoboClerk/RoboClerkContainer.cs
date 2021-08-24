// Part of this file Copyright (c) Alexandre Mutel. All rights reserved.
// Those parts are licensed under the BSD-Clause 2 license. 
// See the license file in the project root for more information.
#nullable enable
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Syntax;

namespace Markdig.Extensions.RoboClerk
{
    public class RoboClerkContainer : ContainerBlock, IFencedBlock
    {
        public RoboClerkContainer(BlockParser parser) : base(parser)
        {
        }

        public char FencedChar { get; set; }

        public int OpeningFencedCharCount { get; set; }

        public StringSlice TriviaAfterFencedChar { get; set; }

        public string? Info { get; set; }

        public StringSlice UnescapedInfo { get; set; }

        public StringSlice TriviaAfterInfo { get; set; }

        public string? Arguments { get; set; }

        public StringSlice UnescapedArguments { get; set; }

        public StringSlice TriviaAfterArguments { get; set; }

        public NewLine InfoNewLine { get; set; }

        public StringSlice TriviaBeforeClosingFence { get; set; }

        public int ClosingFencedCharCount { get; set; }
    }
}