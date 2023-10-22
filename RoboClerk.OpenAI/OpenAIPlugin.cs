using Azure;
using Azure.AI.OpenAI;
using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace RoboClerk.OpenAI
{
    public class OpenAIPlugin : AISystemPluginBase
    {
        private OpenAIClient? openAIClient = null;
        private Dictionary<string,OpenAIPromptTemplate> prompts = new Dictionary<string, OpenAIPromptTemplate>();

        public OpenAIPlugin(IFileSystem fileSystem)
            :base(fileSystem)
        {
            name = "OpenAIPlugin";
            description = "A plugin that interfaces with the OpenAI or Azure OpenAI API.";
        }

        public override void Initialize(IConfiguration configuration)
        {
            base.Initialize(configuration);
            var config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");
            bool useAzureOpenAI = configuration.CommandLineOptionOrDefault("UseAzureOpenAI", GetStringForKey(config, "UseAzureOpenAI", true)).ToUpper() == "TRUE";
            if (useAzureOpenAI)
            {
                string azureOpenAIUri = configuration.CommandLineOptionOrDefault("AzureOpenAIUri", GetStringForKey(config, "AzureOpenAIUri", true));
                string azureOpenAIResourceKey = configuration.CommandLineOptionOrDefault("AzureOpenAIResourceKey", GetStringForKey(config, "AzureOpenAIResourceKey", true));
                openAIClient = new OpenAIClient(new Uri(azureOpenAIUri),new Azure.AzureKeyCredential(azureOpenAIResourceKey));
            }
            else
            {
                string openAIKey = configuration.CommandLineOptionOrDefault("OpenAIKey", GetStringForKey(config, "OpenAIKey", true));
                openAIClient = new OpenAIClient(openAIKey);
            }

        }

        public override string GetFeedback(TraceEntity et, Item item)
        {
            if (prompts.ContainsKey(et.ID))
            {
                return GetRequirementFeedback((RequirementItem)item, prompts[et.ID]);
            }
            throw new NotImplementedException($"AI Feedback about {et.Name} not implemented yet.");
        }

        private ChatRole ConvertStringToChatRole(string role)
        {
            switch(role.ToUpper())
            {
                case "USER": return ChatRole.User;
                case "TOOL": return ChatRole.Tool;
                case "SYSTEM": return ChatRole.System;
                case "ASSISTANT": return ChatRole.Assistant;
                case "FUNCTION": return ChatRole.Function;
                default: throw new Exception($"Unknown role \"{role}\" found in prompt file.");
            }
        }

        private string GetRequirementFeedback(RequirementItem item, OpenAIPromptTemplate template) 
        {
            if (openAIClient != null)
            {
                var chatCompletionOptions = new ChatCompletionsOptions();
                var prompt = template.GetOpenAIPrompt(new Dictionary<string, string>(),item);
                foreach( var message in prompt.messages )
                {
                    chatCompletionOptions.Messages.Add(new ChatMessage(ConvertStringToChatRole(message.role), message.content));
                }
                chatCompletionOptions.Temperature = prompt.temperature;
                chatCompletionOptions.MaxTokens = prompt.max_tokens;
                chatCompletionOptions.PresencePenalty = prompt.presence_penalty;
                chatCompletionOptions.FrequencyPenalty = prompt.frequency_penalty;
                Response<ChatCompletions> completionResponse = openAIClient.GetChatCompletions(prompt.model, chatCompletionOptions);
                return completionResponse.Value.Choices[0].Message.Content;
            }
            throw new Exception("OpenAI Client is null, cannot continue.");
        }

        public override void SetPrompts(List<Document> pts)
        {
            foreach (var prompt in pts)
            {
                prompts[prompt.Title.Split("_AIPrompt")[0]] = new OpenAIPromptTemplate(prompt.ToText());
            }  
        }
    }
}