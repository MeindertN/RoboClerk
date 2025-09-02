using RoboClerk.Core.Configuration;
using System.Collections.Generic;

namespace RoboClerk.AISystem
{
    public interface IAISystemPlugin : IPlugin
    {
        string GetFeedback(TraceEntity et, Item item);

        IEnumerable<DocumentConfig> GetAIPromptTemplates();

        void SetPrompts(List<TextDocument> prompts);
    }
}
