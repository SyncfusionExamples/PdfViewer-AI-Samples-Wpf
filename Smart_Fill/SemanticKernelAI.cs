using Microsoft.Extensions.AI;
using Azure.AI.OpenAI;


namespace WPFPdfViewer_SmartFill
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
        /// <returns>Returns the form data as a string</returns>
        public async Task<string> GetAnswerFromGPT(string systemPrompt)
        {
            if (clientAI != null)
            {
                //// Send the chat completion request to the OpenAI API and await the response.
                var response = await clientAI.GetResponseAsync(systemPrompt);
                return response.ToString();
            }
            return string.Empty;
        }
    }
}
