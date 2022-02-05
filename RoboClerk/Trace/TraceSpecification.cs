using System;
using System.Collections.Generic;
using System.Text;

namespace RoboClerk
{
    public class TraceSpecification : TraceBase
    {
        private bool completeTrace = true;
        private List<string> selectedCategories = new List<string>();

        public TraceSpecification(TraceEntityType source, TraceEntityType target, bool completeTrace)
            : base(source,target)
        {
            this.completeTrace = completeTrace;
        }

        public bool CompleteTrace
        {
            get => completeTrace;
        }

        public List<string> SelectedCategories
        {
            get
            {
                return selectedCategories;
            }

            set
            {
                completeTrace = false;
                selectedCategories = value;
            }
        }
    }
}
