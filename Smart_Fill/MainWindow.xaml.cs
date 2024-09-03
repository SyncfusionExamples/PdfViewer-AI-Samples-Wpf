﻿using Syncfusion.Pdf.Interactive;
using Syncfusion.Pdf.Parsing;
using Syncfusion.Windows.PdfViewer;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
namespace WPFPdfViewer_SmartFill
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields
        private SemanticKernelAI semanticKernelOpenAI;
        Button aIAssistButton;
        DispatcherTimer toolTipTimer; 
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
            pdfViewer.ZoomMode = ZoomMode.FitPage;
            toolTipTimer = new DispatcherTimer();
            toolTipTimer.Interval = new TimeSpan(0, 0, 1);
            toolTipTimer.Tick += ToolTipTimer_Tick;

        }

        /// <summary>
        /// Stop the timer and close the tooltip of the button
        /// </summary>
        /// <param name="sender">Tooltip timer</param>
        /// <param name="e">Event arguments</param>
        private void ToolTipTimer_Tick(object? sender, EventArgs e)
        {
            //Stop the timer and close the tooltip of the button
            toolTipTimer.Stop();
            if (copyUserDataButton1.ToolTip != null)
            {
                (copyUserDataButton1.ToolTip as ToolTip).IsOpen = false;
                copyUserDataButton1.ToolTip = null;
            }
            else if (copyUserDataButton2.ToolTip != null)
            {
                (copyUserDataButton2.ToolTip as ToolTip).IsOpen = false;
                copyUserDataButton2.ToolTip = null;
            }
            else if (copyUserDataButton3.ToolTip != null)
            {
                (copyUserDataButton3.ToolTip as ToolTip).IsOpen = false;
                copyUserDataButton3.ToolTip = null;
            }
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
        /// Click event of the Smart Fill button
        /// </summary>
        /// <param name="sender">Smart Fill button</param>
        /// <param name="e">Event arguments</param>
        private async void SmartFillButton_Click(object sender, RoutedEventArgs e)
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

        /// <summary>
        /// Mouse enter event of the border
        /// </summary>
        /// <param name="sender">Text border</param>
        /// <param name="e">Mouse event arguments</param>
        private void Border_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Border border = sender as Border;
            //Display the copy button on mouse enter of the border
            if (border != null)
            {
                if (border.Name.Contains("1"))
                {
                    copyUserDataButton1.Visibility = Visibility.Visible;
                }
                else if (border.Name.Contains("2"))
                {
                    copyUserDataButton2.Visibility = Visibility.Visible;
                }
                else if (border.Name.Contains("3"))
                {
                    copyUserDataButton3.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// Mouse leave event of the border
        /// </summary>
        /// <param name="sender">Text border</param>
        /// <param name="e">Mouse event arguments</param>
        private void Border_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Border border = sender as Border;
            //Hide the copy button on mouse leave of the border
            if (border != null)
            {
                if (border.Name.Contains("1"))
                {
                    copyUserDataButton1.Visibility = Visibility.Collapsed;
                }
                else if (border.Name.Contains("2"))
                {
                    copyUserDataButton2.Visibility = Visibility.Collapsed;
                }
                else if (border.Name.Contains("3"))
                {
                    copyUserDataButton3.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Click event of the copy button
        /// </summary>
        /// <param name="sender">Copy button</param>
        /// <param name="e">Routed event arguments</param>
        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                //Add a tooltip to the button
                ToolTip toolTip = new ToolTip();
                toolTip.Content = "Text Copied";
                toolTip.PlacementTarget = button;
                button.ToolTip = toolTip;
                toolTip.IsOpen = true;
                toolTipTimer.Start();

                //Clear the clipboard and set the text to the clipboard
                Clipboard.Clear();
                if (button.Name.Contains("1"))
                {

                    Clipboard.SetText(userDataText1.Text);
                }
                else if (button.Name.Contains("2"))
                {
                    Clipboard.SetText(userDataText2.Text);
                }
                else if (button.Name.Contains("3"))
                {
                    Clipboard.SetText(userDataText3.Text);
                }
            }
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
            Button textSearchButton = (Button)toolbar.Template.FindName("PART_ButtonTextSearch", toolbar);
            //Create a new toggle button for AI Assist
            aIAssistButton = new Button();
            TextBlock aIAssistText = new TextBlock();
            aIAssistText.Text = "Smart Fill";
            aIAssistText.FontSize = 14;
            aIAssistButton.Content = aIAssistText;
            aIAssistButton.Click += SmartFillButton_Click;
            aIAssistButton.VerticalAlignment = VerticalAlignment.Center;
            aIAssistButton.Margin = new Thickness(0, 0, 8, 0);
            aIAssistButton.Padding = new Thickness(3);
            // Set the style of the AI Assist button
            aIAssistButton.SetResourceReference(Button.StyleProperty, "WPFPrimaryButtonStyle");
            aIAssistText.SetResourceReference(Button.ForegroundProperty, "PrimaryForeground");
            if(textSearchButton != null)
            {
                copyUserDataButton1.Style = textSearchButton.Style;
                copyUserDataButton2.Style = textSearchButton.Style;
                copyUserDataButton3.Style = textSearchButton.Style;
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

            // Apply the color to the buttons in the toolbar
            ApplyColorToButtons(textSearchButton.Foreground, toolbar);
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
                    separator.Background = border.BorderBrush;
                    userData1.BorderBrush = border.BorderBrush;
                    userData2.BorderBrush = border.BorderBrush;
                    userData3.BorderBrush = border.BorderBrush;
                }
            }

            //Set the background and foreground for the buttons
            aI_Title.Background = background;
            aI_Title.Foreground = foregroundBrush;
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
            // Import the data to the PDF Viewer from the MemoryStream
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
            string copiedDetails = Clipboard.GetText();
            if (!string.IsNullOrEmpty(copiedDetails))
            {
                // Get the XFDF file content from the PDF Viewer
                string loadedFileDetails = ExportFormDetails();
                // Get the hint values for the form fields
                string CustomValues = HintValuesforFields();
                // Create a prompt message for the semantic kernel AI
                string mergePrompt = $"Merge the input data into the XFDF file content. Hint text: {CustomValues}. " +
                                $"Ensure the input data matches suitable field names. " +
                                $"Here are the details: " +
                                $"input data: {copiedDetails}, " +
                                $"XFDF information: {loadedFileDetails}. " +
                                $"Provide the resultant XFDF directly. " +
                                $"Some conditions need to follow: " +
                                $"1. input data is not directly provided as the field name; you need to think and merge appropriately. " +
                                $"2. When comparing input data and field names, ignore case sensitivity. " +
                                $"3. First, determine the best match for the field name. If there isn’t an exact match, use the input data to find a close match. " +
                                $"remove ```xml and remove ``` if it is there in the code.";
                // Get the answer from GPT using the semantic kernel AI
                result = await semanticKernelOpenAI.GetAnswerFromGPT(mergePrompt);
            }

            return result;
        }
        #endregion
    }
}