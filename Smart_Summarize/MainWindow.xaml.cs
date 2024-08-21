using Syncfusion.Windows.PdfViewer;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Syncfusion.Windows.Shared;

namespace Smart_Summarize
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields
        private SemanticKernelAI semanticKernelOpenAI;
        private ToggleButton aIAssistButton;
        private UserChatBox userInputChat;
        private AIResultChatBox aiResultChat;
        private ChatTextBlock chatText;
        #endregion

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
            pdfViewer.Load("../../../Data/GIS Succinctly.pdf");
            sendIcon.Source = new BitmapImage(new Uri("../../../Data/send_icon.png", UriKind.Relative));
            semanticKernelOpenAI = new SemanticKernelAI("YOUR-AI-KEY");
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Indicate the document is loaded in the PdfViewer
        /// </summary>
        /// <param name="sender">PdfViewer Control</param>
        /// <param name="args">Event arguments</param>
        private void pdfViewer_DocumentLoaded(object sender, EventArgs args)
        {
            //Collapse the AI Assistance when the document is loaded
            if (aIAssistButton != null && aIAssistButton.IsChecked.Value)
            {
                aIAssistButton.IsChecked = false;
            }

            //Clear the chat stack panel when the document is loaded
            chatStack.Children.Clear();
        }

        /// <summary>
        /// Handle the loaded event of the pdfViewer
        /// </summary>
        /// <param name="sender">PdfViewer control</param>
        /// <param name="e">Event arguments</param>
        private void pdfViewer_Loaded(object sender, RoutedEventArgs e)
        {
            // Get the text search stack panel from the toolbar of the PDF Viewer
            DocumentToolbar toolbar = (DocumentToolbar)pdfViewer.Template.FindName("PART_Toolbar", pdfViewer);

            // Add the AI Assistance button to the toolbar
            AddAIAssistanceButton(toolbar);
        }

        private void inputText_GotFocus(object sender, RoutedEventArgs e)
        {
            //Add the default prompt to the input text box
            inputText.Text = "";
            SolidColorBrush textForeGround = inputText.Foreground as SolidColorBrush;
            if (textForeGround != null)
            {
                //Make the text color half transparent for the default prompt
                inputText.Foreground = new SolidColorBrush(Color.FromArgb((byte)((int)textForeGround.Color.A * 2), textForeGround.Color.R, textForeGround.Color.G, textForeGround.Color.B));
            }
        }

        private void inputText_LostFocus(object sender, RoutedEventArgs e)
        {
            if (inputText.Text.Length <= 0)
            {
                AddDefaultPromptToChatBox();
            }
        }
        private void AIAssistButton_Unchecked(object sender, RoutedEventArgs e)
        {
            //Collapse the AI Assistance when the button is unchecked
            summarizeGrid.Visibility = Visibility.Collapsed;
            pdfViewer.Focus();
        }

        private async void AIAssistButton_Checked(object sender, RoutedEventArgs e)
        {
            //Expand the AI Assistance when the button is checked
            summarizeGrid.Visibility = Visibility.Visible;
            inputText.Text = string.Empty;
            //Add the default prompt to the input text box
            AddDefaultPromptToChatBox();
            if (chatStack.Children.Count == 0)
            {
                //Add the initial user chat question to the chat stack
                AddUserChat("Summarize this PDF document");

                loadingCanvas.Visibility = Visibility.Visible;
                loadingIndicator.Header = "Summarizing the PDF...";
                loadingIndicator.Visibility = Visibility.Visible;
                //Extract the text from the PDF document
                await ExtractDetailsFromPDF();

                //Summarize the PDF when the chat stack is empty
                string summaryText = await SummarizePDF();

                //Display the summarized text in the AI Result TextBlock
                AddAIChat(summaryText);
                loadingCanvas.Visibility = Visibility.Collapsed;
                loadingIndicator.Visibility = Visibility.Collapsed;
            }
        }

        private async void sendButton_Click(object sender, RoutedEventArgs e)
        {
            if(!string.IsNullOrEmpty(inputText.Text))
            {
                //Add the user input to the chat stack
                AddUserChat(inputText.Text);
                inputText.Text = string.Empty;
                AddDefaultPromptToChatBox();

                loadingCanvas.Visibility = Visibility.Visible;
                loadingIndicator.Header = "Reviewing Question...";
                loadingIndicator.Visibility = Visibility.Visible;
                //Get the answer from GPT using the semantic kernel for the user input
                string answer = await semanticKernelOpenAI.AnswerQuestion(chatText.Text);
                //Display the answer in the AI Result TextBlock
                AddAIChat(answer);
                loadingCanvas.Visibility = Visibility.Collapsed;
                loadingIndicator.Visibility = Visibility.Collapsed;
            }
        }
        #endregion

        #region Helper methods
        /// <summary>
        /// Add the AI Assistance button to the toolbar.
        /// </summary>
        /// <param name="toolbar">Toolbar of the PdfViewer</param>
        private void AddAIAssistanceButton(DocumentToolbar toolbar)
        {
            // Get the text search stack panel and annotation button from the toolbar
            StackPanel textSeacrchStack = (StackPanel)toolbar.Template.FindName("PART_TextSearchStack", toolbar);
            ToggleButton annotationToggleButton = (ToggleButton)toolbar.Template.FindName("PART_Annotations", toolbar);
            //Create a new toggle button for AI Assist
            aIAssistButton = new ToggleButton();
            TextBlock aIAssistText = new TextBlock();
            aIAssistText.Text = "AI Assist View";
            aIAssistText.FontSize = 14;
            aIAssistButton.Content = aIAssistText;
            aIAssistButton.Checked += AIAssistButton_Checked;
            aIAssistButton.Unchecked += AIAssistButton_Unchecked;
            aIAssistButton.Height = 32;
            aIAssistButton.Margin = new Thickness(0, 0, 8, 0);
            aIAssistButton.Padding = new Thickness(4);
            // Set the style of the AI Assist button
            if (annotationToggleButton != null)
            {
                aIAssistButton.Style = annotationToggleButton.Style;
            }
            // Add AI Assist button to the text search stack of the toolbar
            if (textSeacrchStack.Children != null && textSeacrchStack.Children.Count > 0)
            {
                textSeacrchStack.Children.Insert(0, aIAssistButton);
            }
            else
            {
                textSeacrchStack.Children.Add(aIAssistButton);
            }

            ApplyColorToButtons(aIAssistText.Foreground, toolbar);
        }

        /// <summary>
        /// Apply the color to the buttons in the toolbar.
        /// </summary>
        /// <param name="foregroundBrush">Fore ground color</param>
        private void ApplyColorToButtons(Brush foregroundBrush, DocumentToolbar toolbar)
        {
            // Retrieve the root element of the template
            var rootElement = VisualTreeHelper.GetChild(toolbar, 0) as FrameworkElement;
            Brush background = Brushes.Transparent;
            if (rootElement != null)
            {
                // Traverse the visual tree to find the first Border
                var border = FindVisualChild<Border>(rootElement);
                if (border != null && border.Name != "Part_AnnotationToolbar")
                {
                    background = border.Background;
                }
            }
            //Set the background and foreground for the buttons
            aI_Title.Background = background;
            aI_Title.Foreground = foregroundBrush;
            sendButton.Background = background;
            sendButton.Foreground = foregroundBrush;
        }

        /// <summary>
        /// Add the default prompt to the chat box.
        /// </summary>
        private void AddDefaultPromptToChatBox()
        {
            //Add the default prompt to the input text box
            inputText.Text = "Type your prompt for assistance...";
            SolidColorBrush textForeGround = inputText.Foreground as SolidColorBrush;
            if (textForeGround != null)
            {
                //Make the text color half transparent for the default prompt
                inputText.Foreground = new SolidColorBrush(Color.FromArgb((byte)((int)textForeGround.Color.A / 2), textForeGround.Color.R, textForeGround.Color.G, textForeGround.Color.B));
            }
        }

        /// <summary>
        /// Method to find the visual child of the parent element.
        /// </summary>
        /// <typeparam name="T">Type of the child</typeparam>
        /// <param name="parent">Parent element</param>
        /// <returns>Returns the specified type of child</returns>
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent != null && VisualTreeHelper.GetChildrenCount(parent) > 0)
            {
                // Traverse the visual tree to find the first child of the specified type
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    // Check if the child is of the specified type
                    if (child is T tChild)
                    {
                        return tChild;
                    }

                    // Recursively search the child elements
                    var result = FindVisualChild<T>(child);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Extract the text details from the PDF document.
        /// </summary>
        /// <returns>Returns nothing</returns>
        private async Task ExtractDetailsFromPDF()
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

            await semanticKernelOpenAI.CreateEmbeddedPage(extractedText.ToArray());
        }

        /// <summary>
        /// Summarize the PDF document with the extracted text using the Semantic Kernel AI.
        /// </summary>
        /// <returns>Returns the summarized content as string</returns>
        private async Task<string> SummarizePDF()
        {
            //Summarize the text using the Semantic Kernel AI
            string summary = await semanticKernelOpenAI.GetAnswerFromGPT("You are a helpful assistant. Your task is to analyze the provided text and generate short summary.");
            return summary;
        }
        #endregion

        #region Chat Creation Methods
        /// <summary>
        /// Add the AI chat to the chat stack.
        /// </summary>
        /// <param name="text">Text to be added</param>
        private void AddAIChat(string text)
        {
            chatText = new ChatTextBlock();
            chatText.ApplyStyle();
            chatText.Text = text;
            aiResultChat = new AIResultChatBox();
            aiResultChat.ApplyStyle();
            aiResultChat.Child = chatText;
            chatStack.Children.Add(aiResultChat);
            aiResultChat.BringIntoView();
        }

        /// <summary>
        /// Add the user chat to the chat stack.
        /// </summary>
        /// <param name="text">Text to be added</param>
        private void AddUserChat(string text)
        {
            chatText = new ChatTextBlock();
            chatText.ApplyStyle();
            chatText.Text = text;
            userInputChat = new UserChatBox();
            string skinManagerStyle = SfSkinManagerExtension.GetBaseThemeName(this);
            userInputChat.Applystyle(skinManagerStyle);
            userInputChat.Child = chatText;
            chatStack.Children.Add(userInputChat);
            userInputChat.BringIntoView();
        }
        #endregion
    }
}