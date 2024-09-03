using Syncfusion.Windows.PdfViewer;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Syncfusion.Windows.Shared;
using System.IO;

namespace Smart_Summarize
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields
        private SemanticKernelAI SemanticKernelOpenAI;
        private ToggleButton AiAssistButton;
        private UserChatBox UserInputChat;
        private AIResultChatBox AiResultChat;
        private ChatTextBlock ChatText;
        #endregion

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
            //Load the PDF document in the PdfViewer
            pdfViewer.Load("../../../Data/GIS Succinctly.pdf");
            //Initialize the Semantic Kernel AI for summarizing the PDF document
            SemanticKernelOpenAI = new SemanticKernelAI("YOUR-AI-KEY");
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
            if (AiAssistButton != null && AiAssistButton.IsChecked.Value)
            {
                AiAssistButton.IsChecked = false;
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
            if (inputText.Text == "Ask a question about this document...")
            {
                inputText.Text = "";
                SolidColorBrush textForeGround = inputText.Foreground as SolidColorBrush;
                if (textForeGround != null)
                {
                    //Make the text color half transparent for the default prompt
                    inputText.Foreground = new SolidColorBrush(Color.FromArgb((byte)((int)textForeGround.Color.A * 2), textForeGround.Color.R, textForeGround.Color.G, textForeGround.Color.B));
                }
            }
        }

        /// <summary>
        /// Lost focus event for the input text box
        /// </summary>
        /// <param name="sender">Text box</param>
        /// <param name="e">Event arguments</param>
        private void inputText_LostFocus(object sender, RoutedEventArgs e)
        {
            //If the input text box is empty, add the default prompt to the chat box
            if (inputText.Text.Length <= 0)
            {
                AddDefaultPromptToChatBox();
            }
        }
        private void AIAssistButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = sender as ToggleButton;
            if (toggleButton != null)
            {
                (toggleButton.Content as System.Windows.Shapes.Path).SetResourceReference(System.Windows.Shapes.Path.FillProperty, "SecondaryForeground");
            }
            //Collapse the AI Assistance when the button is unchecked
            summarizeGrid.Visibility = Visibility.Collapsed;
            pdfViewer.Focus();
        }

        private async void AIAssistButton_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = sender as ToggleButton;
            if (toggleButton != null)
            {
                (toggleButton.Content as System.Windows.Shapes.Path).SetResourceReference(System.Windows.Shapes.Path.FillProperty, "PrimaryForeground");
            }
            //Expand the AI Assistance when the button is checked
            summarizeGrid.Visibility = Visibility.Visible;
            if (inputText.Text != "Ask a question about this document...")
            {
                inputText.Text = string.Empty;
            }
            //Add the default prompt to the input text box
            AddDefaultPromptToChatBox();
            if (chatStack.Children.Count == 0)
            {
                //Add the initial user chat question to the chat stack
                AddUserChat("Summarize this PDF document");

                //Show the loading indicator for summarizing the PDF
                loadingCanvas.Visibility = Visibility.Visible;
                loadingIndicator.Header = "Summarizing the PDF...";
                loadingIndicator.Visibility = Visibility.Visible;

                //Extract the text from the PDF document
                await ExtractDetailsFromPDF();

                //Summarize the PDF when the chat stack is empty
                string summaryText = await SummarizePDF();

                //Display the summarized text in the AI Result TextBlock
                AddAIChat(summaryText);
                //Hide the loading indicator once the summarization is completed
                loadingCanvas.Visibility = Visibility.Collapsed;
                loadingIndicator.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Click event for the send button in the chat box
        /// </summary>
        /// <param name="sender">Send button</param>
        /// <param name="e">Event arguments</param>
        private async void sendButton_Click(object sender, RoutedEventArgs e)
        {
            if(!string.IsNullOrEmpty(inputText.Text))
            {
                //Add the user input to the chat stack
                AddUserChat(inputText.Text);
                inputText.Text = string.Empty;
                AddDefaultPromptToChatBox();

                //Show the loading indicator for reviewing the question
                loadingCanvas.Visibility = Visibility.Visible;
                loadingIndicator.Header = "Reviewing Question...";
                loadingIndicator.Visibility = Visibility.Visible;

                //Get the answer from GPT using the semantic kernel for the user input
                string answer = await SemanticKernelOpenAI.AnswerQuestion(ChatText.Text);
                //Display the answer in the AI Result TextBlock
                AddAIChat(answer);
                //Hide the loading indicator once the answer is displayed
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
            Button textSearchButton = (Button)toolbar.Template.FindName("PART_ButtonTextSearch", toolbar);
            //Create a new toggle button for AI Assist
            AiAssistButton = new ToggleButton();
            //create path for the AI Assist button
            System.Windows.Shapes.Path aIAssistPath= new System.Windows.Shapes.Path();
            aIAssistPath.Data = PathMarkupToGeometry("M12.7393 0.396992C12.6915 0.170185 12.4942 0.00592726 12.2624 0.000154793C12.0307 -0.00561768 11.8254 0.148608 11.7665 0.372757L11.6317 0.884708C11.4712 1.49476 10.9948 1.97121 10.3847 2.13174L9.87275 2.26646C9.66167 2.32201 9.51101 2.50807 9.50057 2.72609C9.49013 2.94411 9.62233 3.14371 9.82714 3.21917L10.5469 3.48434C11.0663 3.67571 11.4646 4.10157 11.6208 4.63265L11.7703 5.14108C11.8343 5.35876 12.0369 5.50604 12.2637 5.49981C12.4905 5.49358 12.6847 5.33539 12.7367 5.11452L12.8292 4.72158C12.9661 4.1398 13.3904 3.66811 13.9545 3.47066L14.6652 3.22193C14.8737 3.14895 15.0096 2.94777 14.9995 2.72708C14.9893 2.50639 14.8356 2.31851 14.6213 2.26493L14.1122 2.13768C13.4623 1.9752 12.9622 1.45598 12.8242 0.800451L12.7393 0.396992ZM11.3796 2.78214C11.7234 2.57072 12.0165 2.28608 12.2378 1.94927C12.458 2.28452 12.7496 2.5685 13.0919 2.77995C12.7482 2.99134 12.4563 3.27526 12.2359 3.60987C12.015 3.27569 11.7229 2.99268 11.3796 2.78214ZM5.30942 1.83391C5.52745 1.82347 5.72704 1.95566 5.8025 2.16048L6.27391 3.44001C6.65666 4.47889 7.50838 5.27542 8.57054 5.58781L9.47441 5.85365C9.68272 5.91492 9.82768 6.10362 9.83317 6.32068C9.83867 6.53773 9.70345 6.73353 9.49851 6.80526L8.23512 7.24746C7.10689 7.64235 6.25821 8.58572 5.98442 9.74929L5.82004 10.4479C5.76807 10.6687 5.57388 10.8269 5.34707 10.8331C5.12025 10.8394 4.91767 10.6921 4.85365 10.4744L4.58781 9.57055C4.27542 8.50839 3.47889 7.65666 2.44001 7.27391L1.16048 6.8025C0.955663 6.72705 0.823467 6.52745 0.833905 6.30942C0.844343 6.0914 0.995002 5.90534 1.20609 5.8498L2.11623 5.61029C3.33634 5.28922 4.28922 4.33634 4.61029 3.11623L4.84979 2.20609C4.90534 1.995 5.0914 1.84434 5.30942 1.83391ZM5.39094 3.92847C4.93428 5.04593 4.04593 5.93429 2.92847 6.39094C3.985 6.82147 4.83251 7.63474 5.30901 8.65751C5.79435 7.61107 6.66906 6.78063 7.76225 6.35619C6.69032 5.88974 5.83607 5.02082 5.39094 3.92847ZM11.5124 7.00016C11.7442 7.00593 11.9415 7.17019 11.9893 7.39699L12.1025 7.93494C12.2997 8.87141 13.0141 9.61316 13.9426 9.84526L14.6213 10.0149C14.8356 10.0685 14.9893 10.2564 14.9995 10.4771C15.0096 10.6978 14.8737 10.8989 14.6652 10.9719L13.7176 11.3036C12.9117 11.5856 12.3056 12.2595 12.11 13.0906L11.9867 13.6145C11.9347 13.8354 11.7405 13.9936 11.5137 13.9998C11.2869 14.006 11.0843 13.8588 11.0203 13.6411L10.8209 12.9632C10.5978 12.2045 10.0288 11.5961 9.28679 11.3227L8.32714 10.9692C8.12233 10.8937 7.99013 10.6941 8.00057 10.4761C8.01101 10.2581 8.16167 10.072 8.37275 10.0165L9.05536 9.83684C9.92686 9.6075 10.6075 8.92687 10.8368 8.05536L11.0165 7.37276C11.0754 7.14861 11.2807 6.99438 11.5124 7.00016ZM11.4838 9.10985C11.1444 9.72499 10.6264 10.2254 9.9977 10.543C10.6262 10.8597 11.1422 11.3576 11.4815 11.9678C11.8194 11.3576 12.3344 10.8584 12.9632 10.5402C12.3367 10.2219 11.8215 9.72215 11.4838 9.10985Z");
            aIAssistPath.Fill = textSearchButton.Foreground;
            aIAssistPath.Height = 14;
            aIAssistPath.Width = 17;
            AiAssistButton.Content = aIAssistPath;
            AiAssistButton.Checked += AIAssistButton_Checked;
            AiAssistButton.Unchecked += AIAssistButton_Unchecked;
            AiAssistButton.Height = 32;
            AiAssistButton.Margin = new Thickness(0, 0, 8, 0);
            AiAssistButton.Padding = new Thickness(6,0,6,0);
            // Set the style of the AI Assist button
            AiAssistButton.SetResourceReference(ToggleButton.StyleProperty, "WPFToggleButtonStyle");
            if(textSearchButton != null)
            {
                sendButton.Style = textSearchButton.Style;
            }
            // Add AI Assist button to the text search stack of the toolbar
            if (textSeacrchStack.Children != null && textSeacrchStack.Children.Count > 0)
            {
                textSeacrchStack.Children.Insert(0, AiAssistButton);
            }
            else
            {
                textSeacrchStack.Children.Add(AiAssistButton);
            }

            //Apply the color to the buttons added in the toolbar
            ApplyColorToButtons(textSearchButton.Foreground, toolbar);
        }

        #region Path Geometry Helper Methods
        internal Geometry PathMarkupToGeometry(string pathMarkup)
        {
            string xaml =
            "<Path " +
            "xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" +
            "<Path.Data>" + pathMarkup + "</Path.Data></Path>";
            var path = System.Windows.Markup.XamlReader.Load(GenerateStreamFromString(xaml)) as System.Windows.Shapes.Path;
            // Detach the PathGeometry from the Path
            Geometry geometry = path.Data;
            path.Data = null;
            return geometry;
        }
        internal static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
        #endregion

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
                    aI_Title.BorderBrush = border.BorderBrush;
                    aI_Title.BorderThickness = border.BorderThickness;
                    seperator.Background = border.BorderBrush;
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
            SolidColorBrush textForeGround = inputText.Foreground as SolidColorBrush;
            if (textForeGround != null && string.IsNullOrEmpty(inputText.Text))
            {
                //Make the text color half transparent for the default prompt
                inputText.Foreground = new SolidColorBrush(Color.FromArgb((byte)((int)textForeGround.Color.A / 2), textForeGround.Color.R, textForeGround.Color.G, textForeGround.Color.B));
            }

            //Add the default prompt to the input text box
            inputText.Text = "Ask a question about this document...";
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

            await SemanticKernelOpenAI.CreateEmbeddedPage(extractedText.ToArray());
        }

        /// <summary>
        /// Summarize the PDF document with the extracted text using the Semantic Kernel AI.
        /// </summary>
        /// <returns>Returns the summarized content as string</returns>
        private async Task<string> SummarizePDF()
        {
            //Summarize the text using the Semantic Kernel AI
            string summary = await SemanticKernelOpenAI.GetAnswerFromGPT("You are a helpful assistant. Your task is to analyze the provided text and generate short summary as a plain text.");
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
            ChatText = new ChatTextBlock();
            ChatText.ApplyStyle();
            ChatText.Text = text;
            AiResultChat = new AIResultChatBox();
            AiResultChat.ApplyStyle();
            AiResultChat.Child = ChatText;
            chatStack.Children.Add(AiResultChat);
            AiResultChat.BringIntoView();
        }

        /// <summary>
        /// Add the user chat to the chat stack.
        /// </summary>
        /// <param name="text">Text to be added</param>
        private void AddUserChat(string text)
        {
            ChatText = new ChatTextBlock();
            ChatText.ApplyStyle();
            ChatText.Text = text;
            UserInputChat = new UserChatBox();
            string skinManagerStyle = SfSkinManagerExtension.GetBaseThemeName(this);
            UserInputChat.Applystyle(skinManagerStyle);
            UserInputChat.Child = ChatText;
            chatStack.Children.Add(UserInputChat);
            UserInputChat.BringIntoView();
        }
        #endregion
    }
}