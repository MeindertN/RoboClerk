using RoboClerk.Core;

namespace RoboClerk.Server.Services
{
    internal class SimpleRoboClerkTag : IRoboClerkTag
    {
        private readonly Dictionary<string, string> parameters;

        public SimpleRoboClerkTag(string source, string? contentCreatorId, Dictionary<string, string> parameters)
        {
            if (Enum.TryParse<DataSource>(source, out var dataSource))
            {
                Source = dataSource;
            }
            else
            {
                Source = DataSource.Unknown;
            }
            
            ContentCreatorID = contentCreatorId ?? string.Empty;
            this.parameters = parameters ?? new Dictionary<string, string>();
            Contents = string.Empty;
        }

        public DataSource Source { get; }
        public string ContentCreatorID { get; }
        public string Contents { get; set; }
        public bool Inline => false; // For simple implementation, assume not inline
        
        public IEnumerable<string> Parameters => parameters.Keys;

        public string GetParameterOrDefault(string parameterName, string defaultValue = "")
        {
            return parameters.TryGetValue(parameterName, out var value) ? value : defaultValue;
        }

        public void UpdateContent(string newContent)
        {
            Contents = newContent;
        }

        public bool HasParameter(string parameterName)
        {
            return parameters.ContainsKey(parameterName);
        }

        public IEnumerable<IRoboClerkTag> ProcessNestedTags()
        {
            // For this simple implementation, we don't process nested tags
            return Enumerable.Empty<IRoboClerkTag>();
        }
    }
}