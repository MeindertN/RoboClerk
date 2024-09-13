using System.Collections.Generic;
using RoboClerk.AISystem;
using Tomlet;

namespace RoboClerk
{
    public class OpenAIChat
    {
        public string model { get; set; }
        public List<Message> messages { get; set; }
        public float temperature { get; set; }
        public int max_tokens { get; set; }
        public int top_p { get; set; }
        public int frequency_penalty { get; set; }
        public int presence_penalty { get; set; }
    }

    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class OpenAIPromptTemplate : PromptTemplate
    {
        public OpenAIPromptTemplate(string template)
            : base(template)
        {
            
        }

        public OpenAIChat GetOpenAIPrompt(Dictionary<string, string> parameters)
        {
            string basePrompt = GetPrompt(parameters);
            return TomletMain.To<OpenAIChat>(basePrompt);
        }

        public OpenAIChat GetOpenAIPrompt<T>(Dictionary<string, string> parameters, T item)
        {
            string basePrompt = GetPrompt(parameters, item);
            return TomletMain.To<OpenAIChat>(basePrompt);
        }
    }
}
