using Syncfusion.UI.Xaml.Chat;
using Syncfusion.Windows.PdfViewer;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Smart_Summarize
{
    internal class AIAssitViewModel : INotifyPropertyChanged
    {
        #region Fields
        private ObservableCollection<object> chats;
        private ObservableCollection<string> suggestion;
        private Author currentUser;
        private PdfViewerControl pdfViewer;
        MicrosoftAIExtension microsoftAIExtension;
        StringBuilder processedText = new StringBuilder();
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the collection of chat messages.
        /// </summary>
        public ObservableCollection<object> Chats
        {
            get
            {
                return chats;
            }
            set
            {
                chats = value;
                RaisePropertyChanged("Messages");
            }
        }
        /// <summary>
        /// Gets or sets the collection of chat suggestions.
        /// </summary>
        public ObservableCollection<string> Suggestion
        {
            get
            {
                return suggestion;
            }
            set
            {
                suggestion = value;
                RaisePropertyChanged("Suggestion");
            }
        }
        /// <summary>
        /// Gets or sets the current user of the chat.
        /// </summary>
        public Author CurrentUser
        {
            get
            {
                return currentUser;
            }
            set
            {
                currentUser = value;
                RaisePropertyChanged("CurrentUser");
            }
        }
        /// <summary>
        /// Raises the PropertyChanged event to notify the UI of property changes.
        /// </summary>
        /// <param name="propName">The name of the property that changed.</param>
        public void RaisePropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
        #endregion
        #region constructor
        public AIAssitViewModel(PdfViewerControl viewer)
        {
            pdfViewer = viewer;
            Chats = new ObservableCollection<object>();
            suggestion = new ObservableCollection<string>();
            CurrentUser = new Author() { Name = pdfViewer.CurrentUser };
            microsoftAIExtension = new MicrosoftAIExtension("Your-AI-Key");
            Chats.CollectionChanged += Chats_CollectionChanged;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Generates chat messages by summarizing the PDF document.
        /// </summary>
        public async Task GenerateMessages()
        {
            // Execute the following asynchronously
            await ExtractDetailsFromPDF();

            Chats.Add(new TextMessage
            {
                Author = currentUser,
                DateTime = DateTime.Now,
                Text = "Summarizing the PDF document..."
            });
        }
        /// <summary>
        /// Generate suggestion from the answers
        /// </summary>
        private async Task AddSuggestions(String text)
        {
            string suggestions = await microsoftAIExtension.GetAnswerFromGPT("You are a helpful assistant. Your task is to analyze the answer and ask 3 short one-line suggestion questions that user asks.", text);

            var suggestionList = suggestions.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var suggestion in suggestionList)
            {
                Suggestion.Add(suggestion);
            }
        }
        /// <summary>
        /// Extracts text from each page of the PDF document.
        /// </summary>
        private async Task<string> ExtractDetailsFromPDF()
        {
            List<string> extractedText = new List<string>();
            Syncfusion.Pdf.TextLines textLines = new Syncfusion.Pdf.TextLines();
            //Extract the text from the PDF document
            for (int pageIndex = 0; pageIndex < pdfViewer.PageCount; pageIndex++)
            {
                string text = $"... Page {pageIndex + 1} ...\n";
                text += pdfViewer.ExtractText(pageIndex, out textLines);
                extractedText.Add(text);
            }
            microsoftAIExtension.CreateEmbeddedPage(extractedText.ToArray());

            return extractedText.ToString();
        }
       
        /// <summary>
        /// Handles the event when the chat collection changes.  
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data containing information about the collection change.</param>
        private async void Chats_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.NewItems.Count > 0)
            {
                var item = e.NewItems[0] as ITextMessage;
                if (item != null)
                {
                    if (item.Author.Name == currentUser.Name)
                    {
                        string answer = await microsoftAIExtension.AnswerQuestion(item.Text);
                        Chats.Add(new TextMessage
                        {
                            Author = new Author { Name = "AIAssistant" },
                            DateTime = DateTime.Now,
                            Text = answer
                        });
                        Suggestion.Clear();
                        await AddSuggestions(answer);
                    }
                }
            }
        }
        #endregion
    }
}
