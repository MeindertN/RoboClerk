using System.Collections.Generic;

namespace RoboClerk
{
    public class TraceSpecification : TraceBase
    {
        private bool completeTraceForward = true;
        private bool completeTraceBackward = true;
        private List<string> selCatForward = new List<string>();
        private List<string> selCatBackward = new List<string>();
        private ItemLinkType forwardLink = ItemLinkType.None;
        private ItemLinkType backLink = ItemLinkType.None;

        public TraceSpecification(TraceEntity source, TraceEntity target)
            : base(source, target)
        {
        }

        public bool CompleteTraceForward
        {
            get => completeTraceForward;
            set => completeTraceForward = value;
        }

        public bool CompleteTraceBackward
        {
            get => completeTraceBackward;
            set => completeTraceBackward = value;
        }

        public ItemLinkType ForwardLink
        {
            get => forwardLink;
            set => forwardLink = value;
        }

        public ItemLinkType BackwardLink
        {
            get => backLink;
            set => backLink = value;
        }

        public List<string> SelectedCategoriesForward
        {
            get => selCatForward;
            
            set
            {
                completeTraceForward = false;
                selCatForward = value;
            }
        }

        public List<string> SelectedCategoriesBackward
        {
            get => selCatBackward;
            
            set
            {
                completeTraceBackward = false;
                selCatBackward = value;
            }
        }
    }
}
