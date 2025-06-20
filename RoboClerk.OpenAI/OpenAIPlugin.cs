using Azure;
using Azure.AI.OpenAI;
using RoboClerk.Configuration;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using RoboClerk.AISystem;
using Microsoft.Extensions.DependencyInjection;

namespace RoboClerk.OpenAI
{
    public class OpenAIPlugin : AISystemPluginBase
    {
        private OpenAIClient? openAIClient = null;
        private Dictionary<string,OpenAIPromptTemplate> prompts = new Dictionary<string, OpenAIPromptTemplate>();

        public OpenAIPlugin(IFileProviderPlugin fileSystem)
            :base(fileSystem)
        {
            name = "OpenAIPlugin";
            description = "A plugin that interfaces with the OpenAI or Azure OpenAI API.";
        }

        public override void Initialize(IConfiguration configuration)
        {
            base.Initialize(configuration);
            var config = GetConfigurationTable(configuration.PluginConfigDir, $"{name}.toml");
            bool useAzureOpenAI = configuration.CommandLineOptionOrDefault("UseAzureOpenAI", GetObjectForKey<string>(config, "UseAzureOpenAI", true)).ToUpper() == "TRUE";
            if (useAzureOpenAI)
            {
                string azureOpenAIUri = configuration.CommandLineOptionOrDefault("AzureOpenAIUri", GetObjectForKey<string>(config, "AzureOpenAIUri", true));
                string azureOpenAIResourceKey = configuration.CommandLineOptionOrDefault("AzureOpenAIResourceKey", GetObjectForKey<string>(config, "AzureOpenAIResourceKey", true));
                openAIClient = new OpenAIClient(new Uri(azureOpenAIUri),new Azure.AzureKeyCredential(azureOpenAIResourceKey));
            }
            else
            {
                string openAIKey = configuration.CommandLineOptionOrDefault("OpenAIKey", GetObjectForKey<string>(config, "OpenAIKey", true));
                openAIClient = new OpenAIClient(openAIKey);
            }
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