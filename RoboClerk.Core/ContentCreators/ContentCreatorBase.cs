using RoboClerk.Core.Configuration;
using RoboClerk.Core;
using System;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace RoboClerk.ContentCreators
{
    public abstract class ContentCreatorBase : IContentCreator
    {
        protected static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        protected readonly ITraceabilityAnalysis analysis;
        protected readonly IDataSources data;
        protected readonly IConfiguration configuration;

        public ContentCreatorBase(IDataSources data, ITraceabilityAnalysis analysis, IConfiguration configuration)
        {
            this.data = data;
            this.analysis = analysis;
            this.configuration = configuration;
        }

        public abstract string GetContent(IRoboClerkTag tag, DocumentConfig doc);

        /// <summary>
        /// Gets metadata describing this content creator's capabilities.
        /// Derived classes should override this to provide specific metadata.
        /// </summary>
        public virtual ContentCreatorMetadata GetMetadata()
        {
            // Default implementation returns basic metadata
            // Derived classes should override to provide specific details
            return new ContentCreatorMetadata
            {
                Source = GetType().Name,
                Name = GetType().Name,
                Description = "No description available"
            };
        }

        protected static bool ShouldBeIncluded<T>(IRoboClerkTag tag, T item, PropertyInfo[] properties)
        {
            foreach (var param in tag.Parameters)
            {
                foreach (var prop in properties)
                {
                    if (prop.Name.ToUpper() == param)
                    {
                        var propValue = prop.GetValue(item);
                        if ((propValue?.ToString()?.ToUpper() ?? string.Empty) != tag.GetParameterOrDefault(param, string.Empty).ToUpper())
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        protected static bool CheckUpdateDateTime(IRoboClerkTag tag, Item item)
        {
            foreach (var param in tag.Parameters)
            {
                if (param.ToUpper() == "OLDERTHAN" && DateTime.Compare(item.ItemLastUpdated, Convert.ToDateTime(tag.GetParameterOrDefault(param))) >= 0)
                {
                    return false;
                }
                if (param.ToUpper() == "NEWERTHAN" && DateTime.Compare(item.ItemLastUpdated, Convert.ToDateTime(tag.GetParameterOrDefault(param))) <= 0)
                {
                    return false;
                }
            }
            return true;
        }

        protected void ProcessTraces(TraceEntity docTE, ScriptingBridge dataShare)
        {
            foreach (var trace in dataShare.Traces)
            {
                var item = data.GetItem(trace);
                if (item != null)
                {
                    TraceEntity sourceTE = analysis.GetTraceEntityForID(item.ItemType);
                    analysis.AddTrace(sourceTE, trace, docTE, trace);
                }
                else
                {
                    logger.Warn($"Cannot find item with ID \"{trace}\" as referenced in {docTE.Name}. Possible trace issue.");
                    // TODO: remove the null passing by creating an empty source trace entity
                    analysis.AddTrace(null, trace, docTE, trace);
                }
            }
        }

        protected string TagFieldWithAIComment(string itemID, string input)
        {
            string result = input;
            if (configuration.AIPlugin != string.Empty)
            {
                if (!input.Contains($"[[comment_{itemID}]]"))
                {
                    if (char.IsLetterOrDigit(input[0]))
                    {
                        result = $"[[comment_{itemID}]]{input}";
                    }
                    else
                    {
                        Match match = Regex.Match(input, @"(?<=\s)[A-Za-z0-9]");
                        if (match.Success)
                        {
                            result = input.Insert(match.Index, $"[[comment_{itemID}]]");
                        }
                    }
                }
            }
            return result;
        }

        protected void AddAITagsToContent(StringBuilder result, string input, string teID, string itemID)
        {
            if(configuration.AIPlugin != string.Empty)
            {
                result.AppendLine($"@@@AI:AIFeedback(entity={teID},itemID={itemID})");
                result.AppendLine(input);
                result.AppendLine("@@@");
            }
            else
            {
                result.AppendLine(input);
            }
        }

    }
}
