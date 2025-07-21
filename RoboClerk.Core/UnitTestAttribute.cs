using System;

namespace RoboClerk
{
    [AttributeUsage(
     AttributeTargets.Method,
     AllowMultiple = false)]
    public class UnitTestAttribute : Attribute
    {
        private string purpose;
        private string postcondition;
        private string identifier;
        private string traceid;

        public UnitTestAttribute()
        {

        }

        public string Purpose
        {
            get
            {
                return purpose;
            }
            set
            {
                purpose = value;
            }
        }

        public string PostCondition
        {
            get
            {
                return postcondition;
            }
            set
            {
                postcondition = value;
            }
        }

        public string Identifier
        {
            get
            {
                return identifier;
            }
            set
            {
                identifier = value;
            }
        }

        public string TraceID
        {
            get
            {
                return traceid;
            }
            set
            {
                traceid = value;
            }
        }
    }
}
