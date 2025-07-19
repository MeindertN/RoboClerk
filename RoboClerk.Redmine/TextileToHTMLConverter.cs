using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Textile;

namespace RoboClerk.Redmine
{
    public class TextileToHTMLConverter : TextileConverterBase
    {
        protected override string ConvertTextile(string textile)
        {
            return TextileFormatter.FormatString(textile);
        }
    }
}
