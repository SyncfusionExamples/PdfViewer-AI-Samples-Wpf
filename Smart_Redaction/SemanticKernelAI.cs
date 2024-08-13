﻿using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;

namespace WPFPdfViewerAI_SmartRedaction
{
    internal class SemanticKernelAI
    {

        const string endpoint = "YOUR-AI-ENDPOINT";
        const string deploymentName = "YOUR-DEPLOYMENT-NAME";
        internal string key = string.Empty;

        IChatCompletionService chatCompletionService;
        Kernel kernel;

        /// <summary>
        /// Initializes a new instance of the <see cref="SemanticKernelAI"/> class.
        /// </summary>
        /// <param name="key">Key for the semantic kernal API</param>
        public SemanticKernelAI(string key)
        {
            this.key = key;
            var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(deploymentName, endpoint, key);
            kernel = builder.Build();
            chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        }

        /// <summary>
        /// Method to get the answer from GPT using the semantic kernel
        /// </summary>
        /// <param name="systemPrompt">Prompt for the system message</param>
        /// <param name="userText">Input text for the user message</param>
        /// <returns>Returns the sensitive informations as a list</returns>
        public async Task<string> GetAnswerFromGPT(string systemPrompt, string userText)
        {
            var history = new ChatHistory();
            history.AddSystemMessage(systemPrompt);
            history.AddUserMessage(userText);
            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };
            var result = await chatCompletionService.GetChatMessageContentAsync(
            history,
            executionSettings: openAIPromptExecutionSettings,
            kernel: kernel);

            return result.ToString();
        }
    }
}