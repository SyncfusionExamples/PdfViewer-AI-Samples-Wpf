using Microsoft.Extensions.AI;
using Azure.AI.OpenAI;
using System.Text;
using SmartComponents.LocalEmbeddings;
namespace Smart_Summarize
{
    internal class MicrosoftAIExtension
    {

        const string endpoint = "YOUR-AI-ENDPOINT";
        const string deploymentName = "YOUR-DEPLOYMENT-NAME";
        internal string key = string.Empty;

        private IChatClient clientAI;
        public Dictionary<string, EmbeddingF32>? PageEmbeddings { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftAIExtension"/> class.
        /// </summary>
        /// <param name="key">Key for the semantic kernal API</param>
        public MicrosoftAIExtension(string key)
        {
            clientAI = new AzureOpenAIClient(new System.Uri(endpoint), new System.ClientModel.ApiKeyCredential(key)).AsChatClient(deploymentName);
        }

        /// <summary>
        /// Create the embedded page from the extracted chunks in the PDF
        /// </summary>
        /// <param name="chunks">Extracted text from pdfViewer</param>
        /// <returns></returns>
        public async Task CreateEmbeddedPage(string[] chunks)
        {
            var embedder = new LocalEmbedder();
            PageEmbeddings = chunks.Select(x => KeyValuePair.Create(x, embedder.Embed(x))).ToDictionary(k => k.Key, v => v.Value);
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
                List<string> message = PageEmbeddings.Keys.Take(10).ToList();
                var chatMessages = new List<ChatMessage>
                {
                    new ChatMessage(ChatRole.System, systemPrompt),
                    new ChatMessage(ChatRole.User, string.Join(" ", message))
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

        public async Task<string> AnswerQuestion(string question)
        {
            var embedder = new LocalEmbedder();
            var questionEmbedding = embedder.Embed(question);
            var results = LocalEmbedder.FindClosestWithScore(questionEmbedding, PageEmbeddings.Select(x => (x.Key, x.Value)), 5, 0.5f);
            StringBuilder builder = new StringBuilder();
            foreach (var result in results)
            {
                builder.AppendLine(result.Item);
            }
            string message = builder.ToString();
            var answer = await GetAnswerFromGPT("You are a helpful assistant. Use the provided PDF document pages and pick a precise page to answer the user question. Provide the answer in plain text without any special formatting or Markdown syntax. Pages: " + message, question);

            return answer;
        }
    }
}
