using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RoboClerk.Redmine
{
    /// <summary>
    /// Base class for Textile converters that provides RoboClerk tag protection functionality.
    /// </summary>
    public abstract class TextileConverterBase : ITextileConverter
    {
        /// <summary>
        /// Converts Textile content while protecting RoboClerk tags from being processed.
        /// </summary>
        /// <param name="textile">The Textile formatted string.</param>
        /// <returns>The converted string with RoboClerk tags preserved.</returns>
        public string Convert(string textile)
        {
            if (textile == null)
                throw new ArgumentNullException(nameof(textile));

            // Store RoboClerk tags with unique placeholders to protect them from conversion
            Dictionary<string, string> roboClerkTagContents = new Dictionary<string, string>();
            int roboClerkTagIndex = 0;

            // Extract and replace RoboClerk tags with placeholders
            // Match both inline tags (@@tag@@) and block tags (@@@block@@@)
            // Block tags can span multiple lines, so we need to use non-greedy matching
            textile = Regex.Replace(textile, @"@@@(.*?)@@@", m =>
            {
                string content = m.Groups[1].Value;
                string placeholder = $"ROBOCLERKBLOCKPLACEHOLDER{roboClerkTagIndex}";
                roboClerkTagContents[placeholder] = m.Value; // Store the complete tag including @@@
                roboClerkTagIndex++;
                return placeholder;
            }, RegexOptions.Singleline);

            textile = Regex.Replace(textile, @"@@([^@]+)@@", m =>
            {
                string content = m.Groups[1].Value;
                string placeholder = $"ROBOCLERKINLINEPLACEHOLDER{roboClerkTagIndex}";
                roboClerkTagContents[placeholder] = m.Value; // Store the complete tag including @@
                roboClerkTagIndex++;
                return placeholder;
            }, RegexOptions.Singleline);

            // Perform the actual conversion using the derived class implementation
            string convertedText = ConvertTextile(textile);

            // Restore RoboClerk tags
            foreach (var placeholder in roboClerkTagContents.Keys)
            {
                convertedText = convertedText.Replace(placeholder, roboClerkTagContents[placeholder]);
            }

            return convertedText;
        }

        /// <summary>
        /// Performs the actual Textile conversion. This method must be implemented by derived classes.
        /// </summary>
        /// <param name="textile">The Textile content with RoboClerk tags replaced by placeholders.</param>
        /// <returns>The converted content.</returns>
        protected abstract string ConvertTextile(string textile);
    }
} 