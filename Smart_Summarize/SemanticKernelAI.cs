using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using System.Text;
using SmartComponents.LocalEmbeddings;

namespace Smart_Summarize
{
    internal class SemanticKernelAI
    {

        const string endpoint = "YOUR-AI-ENDPOINT";
        const string deploymentName = "YOUR-DEPLOYMENT-NAME";
        internal string key = string.Empty;

        IChatCompletionService chatCompletionService;
        Kernel kernel;

        public Dictionary<string, EmbeddingF32>? PageEmbeddings { get; set; }

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
            List<string> message = PageEmbeddings.Keys.Take(10).ToList();
            var history = new ChatHistory();
            history.AddSystemMessage(systemPrompt);
            history.AddUserMessage(string.Join(" ", message));
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

        /// <summary>
        /// Method to get the answer from GPT using the semantic kernel
        /// </summary>
        /// <param name="systemPrompt">Prompt for the system message</param>
        /// <param name="userText">User text for the AI</param>
        /// <returns>Returns the form data as a string</returns>
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
            var answer = await GetAnswerFromGPT("You are a helpful assistant. Use the provided PDF document pages and pick a precise page to answer the user question, proivde a reference at the bottom of the content with page numbers like ex: Reference: [20,21,23]. Pages: " + message, question);

            return answer;
        }
    }
}
