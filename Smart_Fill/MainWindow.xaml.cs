using Syncfusion.Pdf.Interactive;
using Syncfusion.Pdf.Parsing;
using Syncfusion.Windows.PdfViewer;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPFPdfViewer_SmartFill
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields
        private SemanticKernelAI semanticKernelOpenAI;
        ToggleButton aIAssistButton;
        #endregion

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
            //Initialize the Semantic Kernel AI
            semanticKernelOpenAI = new SemanticKernelAI("YOUR-AI-KEY");
            //Load the PDF document in the PDF Viewer
            pdfViewer.Load("../../../Data/form_document_new.pdf");
            //Zoom the PDF Viewer to 50% to view the entire page
            pdfViewer.ZoomTo(50);
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handle the loaded event of the pdfViewer
        /// </summary>
        /// <param name="sender">PdfViewer control</param>
        /// <param name="e">Event arguments</param>
        private void pdfViewer_Loaded(object sender, RoutedEventArgs e)
        {
            // Get the text search stack panel from the toolbar of the PDF Viewer
            DocumentToolbar toolbar = (DocumentToolbar)pdfViewer.Template.FindName("PART_Toolbar", pdfViewer);
            // Collapse the open menu in the toolbar
            CollapseOpenMenu(toolbar);
            // Add the AI Assistance button to the toolbar
            AddAIAssistanceButton(toolbar);
        }

        /// <summary>
        /// Document loaded event of the pdfViewer
        /// </summary>
        /// <param name="sender">PdfViewer control</param>
        /// <param name="args">Event arguments</param>
        private void pdfViewer_DocumentLoaded(object sender, EventArgs args)
        {
            //Collapse the AI Assistance when the document is loaded
            if (aIAssistButton.IsChecked.Value)
            {
                CollapseAIAssistance();
                aIAssistButton.IsChecked = false;
            }
        }

        /// <summary>
        /// Unchecked event of the AI Assist button
        /// </summary>
        /// <param name="sender">AI Assistance button</param>
        /// <param name="e">Event arguments</param>
        private void AIAssistButton_Unchecked(object sender, RoutedEventArgs e)
        {
            CollapseAIAssistance();
        }

        /// <summary>
        /// Checked event of the AI Assist button
        /// </summary>
        /// <param name="sender">AI Assistance button</param>
        /// <param name="e">event arguments</param>
        private void AIAssistButton_Checked(object sender, RoutedEventArgs e)
        {
            //Display the AI assistance grid
            smartFillGrid.Visibility = Visibility.Visible;
            userDetailsComboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Selection changed event of the user details combo box
        /// </summary>
        /// <param name="sender">Combo box</param>
        /// <param name="e">Selection event arguments</param>
        private void userDetailsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox userDetailsMenu = sender as ComboBox;
            if (userDetailsMenu != null && userDetailsTextBox != null)
            {
                //Set the user details based on the selected index on the text box
                if (userDetailsMenu.SelectedIndex == 0)
                {
                    userDetailsTextBox.Text = "Hi, this is John. You can contact me at john123@gmail.com. I am male, born on February 20, 2005. I want to subscribe to a newspaper and learn courses, specifically a Machine Learning course. I am from Alaska.";
                }
                else if (userDetailsMenu.SelectedIndex == 1)
                {
                    userDetailsTextBox.Text = "S David here. You can reach me at David123@gmail.com. I am male, born on March 15, 2003. I would like to subscribe to a newspaper and am interested in taking a Digital Marketing course. I am from New York.";
                }
                else if (userDetailsMenu.SelectedIndex == 2)
                {
                    userDetailsTextBox.Text = "Hi, this is Alice. You can contact me at alice456@gmail.com. I am female, born on July 15, 1998. I want to unsubscribe from a newspaper and learn courses, specifically a Cloud Computing course. I am from Texas.";
                }

                //Make the user details text box read-only
                userDetailsTextBox.IsReadOnly = true;
            }
        }

        /// <summary>
        /// Click event of the Smart Fill button
        /// </summary>
        /// <param name="sender">Smart Fill button</param>
        /// <param name="e">Event arguments</param>
        private async void smartFillButton_Click(object sender, RoutedEventArgs e)
        {
            //Display the loading indicator
            loadingCanvas.Visibility = Visibility.Visible;
            loadingIndicator.Visibility = Visibility.Visible;

            // Get the answer from GPT using the semantic kernel AI
            string result = await GetDataFromAI();
            //Import the data to the PDF Viewer
            ImportDataToPdfViewer(result);

            // Hide the loading indicator
            loadingCanvas.Visibility = Visibility.Collapsed;
            loadingIndicator.Visibility = Visibility.Collapsed;
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Exports the form details from the PDF Viewer in XFDF format.
        /// </summary>
        /// <returns></returns>
        private string ExportFormDetails()
        {
            if (pdfViewer.LoadedDocument != null && pdfViewer.LoadedDocument.Form != null)
            {
                // Create a new memory stream and use it to export the form data
                MemoryStream stream = new MemoryStream();
                // Export form data in XFDF format to the stream
                pdfViewer.LoadedDocument.Form.ExportEmptyFields = true;
                pdfViewer.LoadedDocument.Form.ExportData(stream, Syncfusion.Pdf.Parsing.DataFormat.XFdf, pdfViewer.DocumentInfo.FileName);
                if (stream != null)
                {
                    stream.Position = 0;
                    // Create a StreamReader to read from the stream
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }

            return "";
        }

        /// <summary>
        /// Provides the hint values for the form fields to the semantic kernel AI.
        /// </summary>
        /// <returns></returns>
        private string HintValuesforFields()
        {
            string? hintData = null;
            if(pdfViewer.LoadedDocument == null || pdfViewer.LoadedDocument.Form == null || pdfViewer.LoadedDocument.Form.Fields.Count <= 0)
            {
                return "";
            }
            // Iterate through each form field in the PDF viewer
            foreach (PdfField field in pdfViewer.LoadedDocument.Form.Fields)
            {
                if (field is PdfLoadedComboBoxField)
                {
                    PdfLoadedComboBoxField combo = field as PdfLoadedComboBoxField;
                    // Append ComboBox name and items to the hintData string
                    hintData += "\n" + combo.Name + " : Collection of Items are :";
                    foreach (PdfLoadedListItem item in combo.Values)
                    {
                        hintData += item.Text + ", ";
                    }
                }
                else if (field is PdfLoadedRadioButtonListField)
                {
                    PdfLoadedRadioButtonListField? radio = field as PdfLoadedRadioButtonListField;
                    // Append RadioButton name and items to the hintData string
                    hintData += "\n" + radio.Name + " : Collection of Items are :";
                    foreach (PdfLoadedRadioButtonItem item in radio.Items)
                    {
                        hintData += item.Value + ", ";
                    }
                }
                else if (field is PdfLoadedListBoxField)
                {
                    PdfLoadedListBoxField? list = field as PdfLoadedListBoxField;
                    // Append ListBox name and items to the hintData string
                    hintData += "\n" + list.Name + " : Collection of Items are :";
                    foreach (PdfLoadedListItem item in list.Values)
                    {
                        hintData += item.Text + ", ";
                    }
                }
                else if (field.Name.Contains("Date") || field.Name.Contains("dob") || field.Name.Contains("date"))
                {
                    // Append instructions for date format to the hintData string
                    hintData += "\n" + field.Name + " : Write Date in MMM/dd/YYYY format";
                }
                
                // Append other form field names to the hintData string
            }
            // Return the hintData string if not null, otherwise return an empty string
            if (hintData != null)
            {
                return hintData;
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Collapse the AI assistance grid.
        /// </summary>
        private void CollapseAIAssistance()
        {
            smartFillGrid.Visibility = Visibility.Collapsed;
            userDetailsTextBox.Text = string.Empty;
            userDetailsComboBox.SelectedIndex = -1;
            pdfViewer.Focus();
        }

        /// <summary>
        /// Collapse the open menu in the toolbar.
        /// </summary>
        /// <param name="toolbar">Toolbar of the PdfViewer</param>
        private void CollapseOpenMenu(DocumentToolbar toolbar)
        {
            //Get the instance of the file menu button using its template name.
            ToggleButton FileButton = (ToggleButton)toolbar.Template.FindName("PART_FileToggleButton", toolbar);
            //Get the instance of the file menu button context menu and the item collection.
            ContextMenu FileContextMenu = FileButton.ContextMenu;
            foreach (MenuItem FileMenuItem in FileContextMenu.Items)
            {
                //Get the instance of the open menu item using its template name and disable its visibility.
                if (FileMenuItem.Name == "PART_OpenMenuItem")
                    FileMenuItem.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

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
            aIAssistText.Text = "Smart Fill";
            aIAssistText.FontSize = 14;
            aIAssistButton.Content = aIAssistText;
            aIAssistButton.Checked += AIAssistButton_Checked;
            aIAssistButton.Unchecked += AIAssistButton_Unchecked; ;
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

            ApplyColorToButtons(aIAssistText.Foreground);
        }

        /// <summary>
        /// Apply the color to the buttons in the toolbar.
        /// </summary>
        /// <param name="foregroundBrush">Fore ground color</param>
        private void ApplyColorToButtons(Brush foregroundBrush)
        {
            //Create the linear gradient brush for the loading indicator
            LinearGradientBrush linearGradientBrush = new LinearGradientBrush();
            linearGradientBrush.StartPoint = new System.Windows.Point(0.5, 0);
            linearGradientBrush.EndPoint = new System.Windows.Point(0.5, 1);
            linearGradientBrush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(0xFF, 0xFE, 0xFE, 0xFE), 0.027));
            linearGradientBrush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(0xFF, 0xFE, 0xFE, 0xFE), 0.029));
            linearGradientBrush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(0xFF, 0xF0, 0xF0, 0xF0), 0.498));
            linearGradientBrush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(0xE2, 0xE2, 0xE2, 0xE2), 0.966));
            linearGradientBrush.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(0xE2, 0xE2, 0xE2, 0xE2), 0.968));

            //Set the background and foreground for the buttons
            aI_Title.Background = linearGradientBrush;
            aI_Title.Foreground = foregroundBrush;
            smartFillButton.Background = linearGradientBrush;
            smartFillButton.Foreground = foregroundBrush;
        }

        /// <summary>
        /// Import the data to the PDF Viewer.
        /// </summary>
        /// <param name="resultData">Result from the AI</param>
        private async void ImportDataToPdfViewer(string resultData)
        {
            var inputFileStream = new MemoryStream();
            // Write the merged XFDF content to the MemoryStream
            var writer = new StreamWriter(inputFileStream, leaveOpen: true);
            await writer.WriteAsync(resultData);
            await writer.FlushAsync();
            inputFileStream.Position = 0;
            byte[] formData = new byte[inputFileStream.Length];
            inputFileStream.Read(formData, 0, formData.Length);
            pdfViewer.ImportFormData(formData, Syncfusion.Pdf.Parsing.DataFormat.XFdf);
        }
        #endregion

        #region OpenAI
        /// <summary>
        /// Method to get the answer from GPT using the semantic kernel AI
        /// </summary>
        /// <returns>Returns the data to be imported in pdfViewer</returns>
        private async Task<string> GetDataFromAI()
        {
            string result = string.Empty;
            // Get the selected user details from the combo box
            string selectedUserDetails = userDetailsTextBox.Text;
            // Get the XFDF file content from the PDF Viewer
            string loadedFileDetails = ExportFormDetails();
            // Get the hint values for the form fields
            string CustomValues = HintValuesforFields();
            // Create a prompt message for the semantic kernel AI
            string mergePrompt = $"Merge the input data into the XFDF file content. Hint text: {CustomValues}. " +
                            $"Ensure the input data matches suitable field names. " +
                            $"Here are the details: " +
                            $"input data: {selectedUserDetails}, " +
                            $"XFDF information: {loadedFileDetails}. " +
                            $"Provide the resultant XFDF directly. " +
                            $"Some conditions need to follow: " +
                            $"1. input data is not directly provided as the field name; you need to think and merge appropriately. " +
                            $"2. When comparing input data and field names, ignore case sensitivity. " +
                            $"3. First, determine the best match for the field name. If there isn’t an exact match, use the input data to find a close match. " +
                            $"remove ```xml and remove ``` if it is there in the code.";
            // Get the answer from GPT using the semantic kernel AI
            result = await semanticKernelOpenAI.GetAnswerFromGPT(mergePrompt);
            return result;
        }
        #endregion
    }
}