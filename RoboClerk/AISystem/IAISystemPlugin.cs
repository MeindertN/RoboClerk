﻿using RoboClerk.Configuration;
using System.Collections.Generic;

namespace RoboClerk.AISystem
{
    public interface IAISystemPlugin
    {
        string GetFeedback(TraceEntity et, Item item);

        IEnumerable<DocumentConfig> GetAIPromptTemplates();

        void SetPrompts(List<Document> prompts);
    }
}
