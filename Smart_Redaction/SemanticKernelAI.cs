
using Microsoft.Extensions.AI;
using Azure.AI.OpenAI;

namespace WPFPdfViewerAI_SmartRedaction
{
    internal class SemanticKernelAI
    {

        const string endpoint = "YOUR-AI-ENDPOINT";
        const string deploymentName = "YOUR-DEPLOYMENT-NAME";
        internal string key = string.Empty;

        private static IChatClient clientAI;

        /// <summary>
        /// Initializes a new instance of the <see cref="SemanticKernelAI"/> class.
        /// </summary>
        /// <param name="key">Key for the semantic kernal API</param>
        public SemanticKernelAI(string key)
        {
            clientAI = new AzureOpenAIClient(new System.Uri(endpoint), new System.ClientModel.ApiKeyCredential(key)).AsChatClient(deploymentName);
        }

        /// <summary>
        /// Method to get the answer from GPT using the semantic kernel
        /// </summary>
        /// <param name="systemPrompt">Prompt for the system message</param>
        /// <param name="userText">Input text for the user message</param>
        /// <returns>Returns the sensitive informations as a list</returns>
        public async Task<string> GetAnswerFromGPT(string systemPrompt, string userText)
        {
            if (clientAI != null)
            {

                var chatMessages = new List<ChatMessage>
                {
                    new ChatMessage(ChatRole.System, systemPrompt),
                    new ChatMessage(ChatRole.User, userText)
                };

                var result = await clientAI.GetResponseAsync(chatMessages);
                return result.ToString();
            }
            return string.Empty;
        }
    }
}
