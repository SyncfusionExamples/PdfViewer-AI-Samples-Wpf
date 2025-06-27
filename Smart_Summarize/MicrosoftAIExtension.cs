using Microsoft.Extensions.AI;
using Azure.AI.OpenAI;
using System.Text;
namespace Smart_Summarize
{
    internal class MicrosoftAIExtension
    {

        const string endpoint = "YOUR-AI-ENDPOINT";
        const string deploymentName = "YOUR-DEPLOYMENT-NAME";
        internal string key = string.Empty;

        private IChatClient clientAI;

        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftAIExtension"/> class.
        /// </summary>
        /// <param name="key">Key for the semantic kernal API</param>
        public MicrosoftAIExtension(string key)
        {
            clientAI = new AzureOpenAIClient(new System.Uri(endpoint), new System.ClientModel.ApiKeyCredential(key)).AsChatClient(deploymentName);
        }

        /// <summary>
        /// Method to get the answer from GPT using the semantic kernel
        /// </summary>
        /// <param name="extractedText">Extracted text from the document</param>
        /// <returns>Returns the form data as a string</returns>
        public async Task<string> GetAnswerFromGPT(string extractedText)
        {
            if (clientAI != null)
            {
                string systemPrompt = "You are a helpful assistant. Your task is to analyze the provided text and generate short summary as a plain text";
                var chatMessages = new List<ChatMessage>
                {
                    new ChatMessage(ChatRole.System, systemPrompt),
                    new ChatMessage(ChatRole.User, extractedText)
                };
                var response = await clientAI.GetResponseAsync(chatMessages);
                return response.ToString();
            }
            return string.Empty;
        }

        /// <summary>
        /// Method to get the answer from GPT using the semantic kernel
        /// </summary>
        /// <param name="systemPrompt">Prompt for the system message</param>
        /// <param name="userText">User text for the AI</param>
        /// <returns>Returns the form data as a string</returns>
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
