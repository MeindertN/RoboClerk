#nullable enable
using RoboClerk.Core.Configuration;
using System;
using System.Collections.Generic;
using RoboClerk.AISystem;
using OpenAI;
using OpenAI.Chat;
using System.Threading.Tasks;

namespace RoboClerk.OpenAI
{
    public class OpenAIPlugin : AISystemPluginBase
    {
        private OpenAIClient? openAIClient = null;
        private Dictionary<string,OpenAIPromptTemplate> prompts = new Dictionary<string, OpenAIPromptTemplate>();

        public OpenAIPlugin(IFileProviderPlugin fileSystem)
            :base(fileSystem)
        {
            SetBaseParam();
        }

        private void SetBaseParam()
        {
            name = "OpenAIPlugin";
            description = "A plugin that interfaces with the OpenAI or Azure OpenAI API.";
        }

        public override void InitializePlugin(IConfiguration configuration)
        {
            base.InitializePlugin(configuration);
            var config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");
            string openAIKey = configuration.CommandLineOptionOrDefault("OpenAIKey", GetObjectForKey<string>(config, "OpenAIKey", true));
            openAIClient = new OpenAIClient(openAIKey);
        }

        // Using base.ConfigureServices implementation which registers this as IAISystemPlugin

        public override string GetFeedback(TraceEntity et, Item item)
        {
            if (prompts.ContainsKey(et.ID))
            {
                return GetRequirementFeedback((RequirementItem)item, prompts[et.ID]);
            }
            throw new NotImplementedException($"AI Feedback about {et.Name} not implemented yet.");
        }

        private ChatMessage CreateChatMessage(string role, string content)
        {
            return role.ToUpper() switch
            {
                "USER" => ChatMessage.CreateUserMessage(content),
                "SYSTEM" => ChatMessage.CreateSystemMessage(content),
                "ASSISTANT" => ChatMessage.CreateAssistantMessage(content),
                "TOOL" => ChatMessage.CreateToolMessage("tool", content),
                "FUNCTION" => ChatMessage.CreateToolMessage("function", content),
                _ => throw new Exception($"Unknown role \"{role}\" found in prompt file."),
            };
        }


        private async Task<string> GetRequirementFeedbackAsync(RequirementItem item, OpenAIPromptTemplate template) 
        {
            if (openAIClient != null)
            {
                var prompt = template.GetOpenAIPrompt(new Dictionary<string, string>(), item);
                
                var messages = new List<ChatMessage>();
                foreach (var message in prompt.messages)
                {
                    messages.Add(CreateChatMessage(message.role, message.content));
                }

                var chatCompletionOptions = new ChatCompletionOptions
                {
                    Temperature = prompt.temperature,
                    MaxOutputTokenCount = prompt.max_tokens,
                    PresencePenalty = prompt.presence_penalty,
                    FrequencyPenalty = prompt.frequency_penalty
                };

                var chatClient = openAIClient.GetChatClient(prompt.model);
                var completionResponse = await chatClient.CompleteChatAsync(messages, chatCompletionOptions);
                var completion = completionResponse.Value;
                return completion.Content[0].Text;
            }
            throw new Exception("OpenAI Client is null, cannot continue.");
        }

        private string GetRequirementFeedback(RequirementItem item, OpenAIPromptTemplate template)
        {
            return GetRequirementFeedbackAsync(item, template).GetAwaiter().GetResult();
        }

        public override void SetPrompts(List<TextDocument> pts)
        {
            foreach (var prompt in pts)
            {
                prompts[prompt.Title.Split("_AIPrompt")[0]] = new OpenAIPromptTemplate(prompt.ToText());
            }  
        }
    }
}