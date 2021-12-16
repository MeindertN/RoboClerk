using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk
{
    public class DocTrace
    {
        private TraceEntityType targetDoc = TraceEntityType.Unknown;
        private bool completeTrace = true;
        private List<string> selectedCatagories = new List<string>();

        public DocTrace(TraceEntityType targetDoc, bool completeTrace)
        {
            this.targetDoc = targetDoc;
            this.completeTrace = completeTrace;
        }

        public TraceEntityType TargetDoc
        {
            get => targetDoc;
        }

        public bool CompleteTrace
        {
            get => completeTrace;
        }

        public List<string> SelectedCatagories
        {
            get
            {
                return selectedCatagories;
            }

            set
            {
                completeTrace = false;
                selectedCatagories = value;
            }
        }
    }
}
