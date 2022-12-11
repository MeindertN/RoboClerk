using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoboClerk
{
    internal class ItemTemplateParser
    {
        private (string, int, int) startSegment = (string.Empty,-1,-1);
        private List<(string,int,int)> segments = new List<(string,int,int)>();

        public ItemTemplateParser(string itemTemplate)
        {
            Parse(itemTemplate);
        }

        public (string,int,int) StartSegment { get { return startSegment; } }

        public IEnumerable<(string,int,int)> Segments { get { return segments; } }

        private void Parse(string itemTemplate)
        {
            segments.Clear();
            int nrOfOpenBrackets = 0;
            bool insideTag = false;
            int segmentStart = 0;
            int segmentEnd = 0;
            StringBuilder segment = new StringBuilder();
            for(int i = 0; i < itemTemplate.Length; i++)
            {
                if (insideTag)
                {
                    if (itemTemplate[i] == '[')
                    {
                        nrOfOpenBrackets++;
                    }
                    else if (itemTemplate[i] == ']')
                    {
                        nrOfOpenBrackets--;
                    }
                    if (nrOfOpenBrackets == 0)
                    {
                        segmentEnd = i;
                        insideTag = false;
                        if (startSegment.Item2 < 0)
                        {
                            startSegment = (segment.ToString(), segmentStart, segmentEnd+1); //+1 to include ]
                        }
                        else
                        {
                            //reverse the order which makes inserting the final evaluated results easier
                            segments.Insert(0, (segment.ToString(), segmentStart, segmentEnd+1)); 
                        }
                    }
                    else
                    {
                        segment.Append(itemTemplate[i]);
                    }
                }
                else
                {
                    if (itemTemplate[i] == '[')
                    {
                        if (i + 5 < itemTemplate.Length && itemTemplate.Substring(i, 5) == "[csx:")
                        {
                            segment.Clear();
                            insideTag = true;
                            segmentStart = i;
                            segmentEnd = i + 5;
                            i += 4;
                            nrOfOpenBrackets = 1;
                            continue;
                        }
                    }
                }
            }
        }
    }
}
