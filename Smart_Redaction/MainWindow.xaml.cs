using Syncfusion.Pdf.Graphics;
using Syncfusion.Windows.PdfViewer;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace WPFPdfViewerAI_SmartRedaction
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields
        private bool isAllInformationsSelected = false;
        private Dictionary<int, List<RectangleF>> redactRegions = new Dictionary<int, List<RectangleF>>();
        private MicrosoftAIExtension microsoftAIExtension;
        ToggleButton aIAssistButton;
        #endregion

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
            // Load the PDF document
            pdfViewer.Load("../../../Data/Confidential_Medical_Record.pdf");

            //Create an instance of the SemanticKernelAI class which initializes the OpenAI client
            microsoftAIExtension = new MicrosoftAIExtension("YOUR-AI-KEY");
        }
        #endregion

        #region Event Handlers
        #region PDF Viewer and Window Events
        /// <summary>
        /// Loaded event of the PDF Viewer control
        /// </summary>
        /// <param name="sender">PDF Viewer</param>
        /// <param name="e">Event arguments</param>
        private void PdfViewer_Loaded(object sender, RoutedEventArgs e)
        {
            // Get the toolbar instance of the PDF Viewer
            DocumentToolbar toolbar = (DocumentToolbar)pdfViewer.Template.FindName("PART_Toolbar", pdfViewer);

            //Add the AI assistance button to the text search stack panel of the PDF Viewer
            AddAIAssistanceButton(toolbar);

            //Set the loading indicator size
            loadingIndicator.Height = this.Height;
        }

        /// <summary>
        /// Document loaded event of the PDF Viewer control
        /// </summary>
        /// <param name="sender">PDFViewer control</param>
        /// <param name="args">Event arguments</param>
        private void pdfViewer_DocumentLoaded(object sender, EventArgs args)
        {
            //Collapse the AI assistance grid when the document is loaded
            if (aIAssistButton.IsChecked.Value)
            {
                CollapseAIAssistance();
                aIAssistButton.IsChecked = false;
            }
        }

        /// <summary>
        /// Size changed event of the window
        /// </summary>
        /// <param name="sender">Main window</param>
        /// <param name="e">Size changed event arguments</param>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Set the updated height of the loading indicator
            loadingIndicator.Height = e.NewSize.Height;
        }
        #endregion

        #region checked events
        /// <summary>
        /// Checked event of the redact options
        /// </summary>
        /// <param name="sender">Redact options checkbox</param>
        /// <param name="e">Event arguments</param>
        private void patternSelection_Checked(object sender, RoutedEventArgs e)
        {
            //Select all the redact options
            person.IsChecked = true;
            address.IsChecked = true;
            organization.IsChecked = true;
            date.IsChecked = true;
            phone.IsChecked = true;
            email.IsChecked = true;
            credit.IsChecked = true;
            account.IsChecked = true;
            ssn.IsChecked = true;
        }

        /// <summary>
        /// Unchecked event of the redact options
        /// </summary>
        /// <param name="sender">Redact options checkbox</param>
        /// <param name="e">Event arguments</param>
        private void patternSelection_Unchecked(object sender, RoutedEventArgs e)
        {
            //Unselect all the redact options
            person.IsChecked = false;
            address.IsChecked = false;
            organization.IsChecked = false;
            date.IsChecked = false;
            phone.IsChecked = false;
            email.IsChecked = false;
            credit.IsChecked = false;
            account.IsChecked = false;
            ssn.IsChecked = false;
        }

        /// <summary>
        /// Checked event of the select all checkbox
        /// </summary>
        /// <param name="sender">Select all checkbox</param>
        /// <param name="e">Event arguments</param>
        private void selectAll_Checked(object sender, RoutedEventArgs e)
        {
            isAllInformationsSelected = true;
            loadingIndicator.Visibility = Visibility.Visible;
            loadingCanvas.Visibility = Visibility.Visible;
            //Select all the extracted sensitive information checkboxes
            for (int infoIndex = 1; infoIndex < (information_Stack.Children.Count - 1); infoIndex++)
            {
                CheckBox checkBox = information_Stack.Children[infoIndex] as CheckBox;
                if (checkBox != null)
                {
                    checkBox.IsChecked = true;
                }
            }

            //Mark all the sensitive information in the PDFViewer
            MarkSensitiveInformation((List<string>)selectAll.Tag);
            loadingIndicator.Visibility = Visibility.Collapsed;
            loadingCanvas.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Unchecked event of the select all checkbox
        /// </summary>
        /// <param name="sender">Select all checkbox</param>
        /// <param name="e">Event arguments</param>
        private void selectAll_Unchecked(object sender, RoutedEventArgs e)
        {
            ClearMarkedRegionForRedaction();
            //Unselect all the extracted sensitive information checkboxes
            for (int infoIndex = 1; infoIndex < (information_Stack.Children.Count - 1); infoIndex++)
            {
                CheckBox checkBox = information_Stack.Children[infoIndex] as CheckBox;
                if (checkBox != null)
                {
                    checkBox.IsChecked = false;
                }
            }

            //Clear the marked regions to redact in the PDFViewer
            for (int pageIndex = 0; pageIndex < pdfViewer.PageCount; pageIndex++)
            {
                pdfViewer.PageRedactor.MarkRegions(pageIndex, new List<RectangleF>());
            }
            pdfViewer.PageRedactor.EnableRedactionMode = true;
        }

        /// <summary>
        /// Unchecked event of the extracted sensitive information checkboxes
        /// </summary>
        /// <param name="sender">Sensitive information checkbox</param>
        /// <param name="e">Event arguments</param>
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (isAllInformationsSelected)
            {
                isAllInformationsSelected = false;
                selectAll.Unchecked -= selectAll_Unchecked;
                selectAll.IsChecked = false;
                selectAll.Unchecked += selectAll_Unchecked;
            }

            //Check the selected sensitive information checkboxes and disable the apply button
            ToggleApplyButton();

            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                Dictionary<int, RectangleF> bounds = (Dictionary<int, RectangleF>)checkBox.Tag;
                if (bounds != null && bounds.Count > 0)
                {
                    //Clear the marked region of the selected sensitive information
                    RemoveMarkedRegionForRedaction(bounds);
                }
            }
        }

        /// <summary>
        /// Checked event of the extracted sensitive information checkboxes
        /// </summary>
        /// <param name="sender">Sensitive information checkbox</param>
        /// <param name="e">Event arguments</param>
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!applyButton.IsEnabled)
            {
                applyButton.IsEnabled = true;
            }
            //Check whether all the extracted sensitive information checkboxes are selected
            if (isAllInformationsSelected)
            {
                return;
            }
            else
            {
                ToggleSelectAllCheckedState();
            }

            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                Dictionary<int, RectangleF> bounds = (Dictionary<int, RectangleF>)checkBox.Tag;
                if (bounds != null && bounds.Count > 0)
                {
                    //Mark the region of the selected sensitive information
                    AddMarkedRegionForRedaction(bounds);

                    //Scroll to the marked region in the PDFViewer
                    if (bounds.Keys.First() <= pdfViewer.PageCount - 1 && bounds.Keys.First() >= 0)
                    {
                        ScrollToMarkedRegion(bounds.Keys.First(), bounds.Values.First());
                    }
                }
            }
        }

        /// <summary>
        /// Checked event of the sensitive information checkbox
        /// </summary>
        /// <param name="sender">Check box</param>
        /// <param name="e">Event arguments</param>
        private void SensitiveInfo_Checked(object sender, RoutedEventArgs e)
        {
            if((patternSelection.IsChecked.Value && !scanButton.IsEnabled) || !scanButton.IsEnabled)
            {
                scanButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Unchecked event of the sensitive information checkbox
        /// </summary>
        /// <param name="sender">Check box</param>
        /// <param name="e">event arguments</param>
        private void SensitiveInfo_Unchecked(object sender, RoutedEventArgs e)
        {
            if (scanButton.IsEnabled)
            {
                scanButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// Unchecked event of the AI assistance button
        /// </summary>
        /// <param name="sender">Ai Assistance button</param>
        /// <param name="e">Event arguments</param>
        private void AIAssistButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = sender as ToggleButton;
            if (toggleButton != null)
            {
                (toggleButton.Content as TextBlock).SetResourceReference(ToggleButton.ForegroundProperty, "SecondaryForeground");
            }
            CollapseAIAssistance();
        }

        /// <summary>
        /// Checked event of the AI assistance button
        /// </summary>
        /// <param name="sender">AI Assistance button</param>
        /// <param name="e">Event arguments</param>
        private void AIAssistButton_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = sender as ToggleButton;
            if (toggleButton != null)
            {
                (toggleButton.Content as TextBlock).SetResourceReference(ToggleButton.ForegroundProperty, "PrimaryForeground");
            }
            // Show the AI assistance grid
            aiGrid.Visibility = Visibility.Visible;
        }
        #endregion

        /// <summary>
        /// Preview mouse down event of the select all checkbox
        /// </summary>
        /// <param name="sender">Check box</param>
        /// <param name="e">Mouse event arguments</param>
        private void selectAll_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!selectAll.IsChecked.Value)
            {
                loadingIndicator.Visibility = Visibility.Visible;
                loadingCanvas.Visibility = Visibility.Visible;
            }
        }

        #region Click Events
        /// <summary>
        /// Click event of the search button of the selected options checkboxes
        /// </summary>
        /// <param name="sender">Smart redact button</param>
        /// <param name="e">Event arguments</param>
        private async void Smart_Scan_Click(object sender, RoutedEventArgs e)
        {
            List<string> selectedItems = new List<string>();
            //Change the visibility of the loading indicator
            loadingIndicator.Visibility = Visibility.Visible;
            loadingCanvas.Visibility = Visibility.Visible;

            //Iterate through the redact options checkboxes
            foreach (UIElement content in contents_Stack.Children)
            {
                CheckBox option = content as CheckBox;
                if (option != null)
                {
                    //Check whether the redact option checkbox is selected
                    if ((bool)option.IsChecked)
                    {
                        if (option.Name == "selection")
                        {
                            //Select all the redact options
                            selectedItems.Add("1. Person Name");
                            selectedItems.Add("2. Address");
                            selectedItems.Add("3. Organization Names");
                            selectedItems.Add("4. Dates (e.g., birthdates, transaction dates)");
                            selectedItems.Add("5. Phone Number");
                            selectedItems.Add("6. Email");
                            selectedItems.Add("7. Credit Card Number");
                            selectedItems.Add("8. Account Number");
                            selectedItems.Add("9. Social Security Number (SSN)");
                            break;
                        }
                        else
                        {
                            //Get the selected redact options checkboxes
                            selectedItems.Add(GetSelectedItems(option.Name));
                        }
                    }
                }
            }

            //Check whether the selected redact options contain any category to redact
            if (selectedItems.Count > 0)
            {
                string extractedText = string.Empty;
                Syncfusion.Pdf.TextLines textLines = new Syncfusion.Pdf.TextLines();
                //Extract the text from the PDF document
                for (int pageIndex = 0; pageIndex < pdfViewer.PageCount; pageIndex++)
                {
                    extractedText += pdfViewer.ExtractText(pageIndex, out textLines);
                }

                //Extract the selected sensitive informations from the extracted text
                List<string> ExtractedDetails = new List<string>();
                ExtractedDetails = await ExtractSelectedInfo(extractedText, selectedItems);

                //Add the extracted sensitive information as checkboxes to the stack panel
                List<string> sensitiveInformations = RemovePrefix(ExtractedDetails, selectedItems);
                AddInformationToStack(sensitiveInformations);
            }

            //Toggle the visibility of the UI elements after the extraction of the sensitive information
            ToggleUIVisibiltity();
        }

        /// <summary>
        /// Click event of the cancel button
        /// </summary>
        /// <param name="sender">Cancel button</param>
        /// <param name="e">Event arguments</param>
        private void cancel_Click(object sender, RoutedEventArgs e)
        {
            //Hide the extracted sensitive information checkboxes
            RemoveInformationFromStack();
            //Clear the marked regions to redact
            ClearMarkedRegionForRedaction();
            //Show the redact options checkboxes
            contents_Stack.Visibility = Visibility.Visible;
            selectAll.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Click event of the apply button
        /// </summary>
        /// <param name="sender">Apply redaction button</param>
        /// <param name="e">Event arguments</param>
        private void apply_Click(object sender, RoutedEventArgs e)
        {
            if (pdfViewer.PageRedactor.EnableRedactionMode)
            {
                //Apply the redaction to the marked regions in the PDF document
                pdfViewer.PageRedactor.ApplyRedaction();
                ClearMarkedRegionForRedaction();
                for (int infoIndex = information_Stack.Children.Count - 2; infoIndex >= 1; infoIndex--)
                {
                    CheckBox checkBox = information_Stack.Children[infoIndex] as CheckBox;
                    if (checkBox != null && (bool)checkBox.IsChecked)
                    {
                        information_Stack.Children.RemoveAt(infoIndex);
                    }
                }

                //Hide the extracted sensitive information checkboxes when all the sensitive information is redacted
                if (information_Stack.Children.Count == 2)
                {
                    information_Grid.Visibility = Visibility.Collapsed;
                    option_Scroll.Visibility = Visibility.Visible;
                    contents_Stack.Visibility = Visibility.Visible;
                    selectAll.IsChecked = false;
                    pdfViewer.PageRedactor.EnableRedactionMode = false;
                }

                //Disable the apply button as none will be selected after redaction
                applyButton.IsEnabled = false;

                //Update the total count of the extracted sensitive information
                int totalInfoCount = information_Stack.Children.Count - 2;
                count_Label.Content = totalInfoCount + " informations found";
            }
        }
        #endregion
        #endregion

        #region Helper methods
        #region Redaction helper Methods
        /// <summary>
        /// Mark the sensitive information to redact in the PDF document
        /// </summary>
        /// <param name="sensitiveInfo">List of sensitive informations</param>
        private void MarkSensitiveInformation(List<string> sensitiveInfo)
        {
            //Find the bounds of the sensitive information the PDF document
            Dictionary<int, List<TextSearchResult>> sensitiveInfoBounds = new Dictionary<int, List<TextSearchResult>>();
            pdfViewer.FindText(sensitiveInfo, out sensitiveInfoBounds);
            //Clear the marked regions to redact
            ClearMarkedRegionForRedaction();
            for (int pageIndex = 0; pageIndex < pdfViewer.PageCount; pageIndex++)
            {
                List<RectangleF> infoBounds = new List<RectangleF>();
                if (sensitiveInfoBounds.ContainsKey(pageIndex))
                {
                    foreach (TextSearchResult result in sensitiveInfoBounds[pageIndex])
                    {
                        infoBounds.Add(result.Bounds);
                    }
                }

                //Mark the regions to be redacted in the PDFViewer
                pdfViewer.PageRedactor.MarkRegions(pageIndex, infoBounds);
                pdfViewer.PageRedactor.EnableRedactionMode = true;
                redactRegions.Add(pageIndex, infoBounds);
            }
        }

        /// <summary>
        /// Clear the marked regions to redact in the PDFViewer
        /// </summary>
        private void ClearMarkedRegionForRedaction()
        {
            redactRegions.Clear();
            redactRegions = new Dictionary<int, List<RectangleF>>();
            pdfViewer.PageRedactor.EnableRedactionMode = false;
        }
        #endregion

        /// <summary>
        /// Method to get the selected redact options checkboxes
        /// </summary>
        /// <param name="name">Name of the checkbox</param>
        /// <returns>Returns the selected redact option</returns>
        private string GetSelectedItems(string name)
        {
            //Get the selected redact options
            string selectedItem = string.Empty;
            switch (name)
            {
                case "person":
                    selectedItem = "1. Person Names";
                    break;
                case "address":
                    selectedItem = "2. Address";
                    break;
                case "organization":
                    selectedItem = "3. Organization Names";
                    break;
                case "date":
                    selectedItem = "4. Dates (e.g., birthdates, transaction dates)";
                    break;
                case "phone":
                    selectedItem = "5. Phone Number";
                    break;
                case "email":
                    selectedItem = "6. Email";
                    break;
                case "credit":
                    selectedItem = "7. Credit Card Number";
                    break;
                case "account":
                    selectedItem = "8. Account Number";
                    break;
                case "ssn":
                    selectedItem = "9. Social Security Number (SSN)";
                    break;
            }

            return selectedItem;
        }

        /// <summary>
        /// Applies the background and foreground color to the buttons
        /// </summary>
        /// <param name="foregroundColor"></param>
        private void ApplyColorforButtons(System.Windows.Media.Brush foregroundColor, DocumentToolbar toolbar)
        {
            // Retrieve the root element of the template
            var rootElement = VisualTreeHelper.GetChild(toolbar, 0) as FrameworkElement;
            System.Windows.Media.Brush background = System.Windows.Media.Brushes.Transparent;
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
            scanButton.Background = background;
            scanButton.Foreground = foregroundColor;
            cancelButton.Background = background;
            cancelButton.Foreground = foregroundColor;
            applyButton.Background = background;
            applyButton.Foreground = foregroundColor;
            aI_Title.Background = background;
            aI_Title.Foreground = foregroundColor;
        }

        /// <summary>
        /// Add the AI assistance button to the text search stack panel of the PDF Viewer
        /// </summary>
        /// <param name="toolbar">oolbar of the pdfViewer</param>
        private void AddAIAssistanceButton(DocumentToolbar toolbar)
        {
            //Get the text search stack panel and the annotations toggle button from the toolbar
            StackPanel textSeacrchStack = (StackPanel)toolbar.Template.FindName("PART_TextSearchStack", toolbar);
            ToggleButton annotationToggleButton = (ToggleButton)toolbar.Template.FindName("PART_Annotations", toolbar);
            // Create a text block for the AI assistance button
            TextBlock aIAssistText = new TextBlock();
            aIAssistText.Text = "Smart Redact";
            aIAssistText.FontSize = 14;
            // Create a toggle button for the AI assistance button
            aIAssistButton = new ToggleButton();
            aIAssistButton.Content = aIAssistText;
            aIAssistButton.Checked += AIAssistButton_Checked;
            aIAssistButton.Unchecked += AIAssistButton_Unchecked;
            aIAssistButton.VerticalAlignment = VerticalAlignment.Center;
            aIAssistButton.Margin = new Thickness(0, 0, 8, 0);
            aIAssistButton.Padding = new Thickness(4);
            // Set the style of the AI Assist button
            aIAssistButton.SetResourceReference(ToggleButton.StyleProperty, "WPFToggleButtonStyle");
            aIAssistText.SetResourceReference(ToggleButton.ForegroundProperty, "SecondaryForeground");
            // Add the AI assistance button to the text search stack panel
            if (textSeacrchStack.Children != null && textSeacrchStack.Children.Count > 0)
            {
                textSeacrchStack.Children.Insert(0, aIAssistButton);
            }
            else
            {
                textSeacrchStack.Children.Add(aIAssistButton);
            }

            //Apply the background and foreground color to the buttons in the application
            ApplyColorforButtons(annotationToggleButton.Foreground, toolbar);
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
        /// Toggle the apply button based on the selected sensitive information
        /// </summary>
        private void ToggleApplyButton()
        {
            bool isAllInformationsUnselected = true;
            for (int infoIndex = 1; infoIndex < information_Stack.Children.Count - 1; infoIndex++)
            {
                CheckBox information = information_Stack.Children[infoIndex] as CheckBox;
                if (information != null && (bool)information.IsChecked)
                {
                    isAllInformationsUnselected = false;
                    break;
                }
            }
            if (isAllInformationsUnselected)
            {
                applyButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// Toggle the visibility of the UI elements
        /// </summary>
        private void ToggleUIVisibiltity()
        {
            //Hide the extracted sensitive information checkboxes when all the sensitive information is redacted
            if (information_Stack.Children.Count <= 2)
            {
                selectAll.Visibility = Visibility.Collapsed;
            }

            //Show the extracted sensitive information checkboxes and hide the redact options checkboxes
            option_Scroll.Visibility = Visibility.Collapsed;
            information_Grid.Visibility = Visibility.Visible;
            contents_Stack.Visibility = Visibility.Collapsed;
            ResetRedactionOptions();

            //Hide the visibility of the loading indicator
            loadingIndicator.Visibility = Visibility.Collapsed;
            loadingCanvas.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Check whether all the extracted sensitive information checkboxes are selected
        /// </summary>
        private void ToggleSelectAllCheckedState()
        {
            isAllInformationsSelected = true;
            for (int infoIndex = 1; infoIndex < information_Stack.Children.Count - 1; infoIndex++)
            {
                CheckBox information = information_Stack.Children[infoIndex] as CheckBox;
                if (information != null && !(bool)information.IsChecked)
                {
                    isAllInformationsSelected = false;
                    break;
                }
            }

            if (isAllInformationsSelected && !(bool)selectAll.IsChecked)
            {
                selectAll.Checked -= selectAll_Checked;
                selectAll.IsChecked = true;
                selectAll.Checked += selectAll_Checked;
            }
        }

        /// <summary>
        /// Add the marked region to redact in the PDFViewer
        /// </summary>
        /// <param name="bounds">Region to be added</param>
        private void AddMarkedRegionForRedaction(Dictionary<int, RectangleF> bounds)
        {
            if (redactRegions.ContainsKey(bounds.Keys.First()))
            {
                redactRegions[bounds.Keys.First()].Add(bounds.Values.First());
            }
            else
            {
                redactRegions.Add(bounds.Keys.First(), new List<RectangleF> { bounds.Values.First() });
            }

            pdfViewer.PageRedactor.MarkRegions(bounds.Keys.First(), redactRegions[bounds.Keys.First()]);
            pdfViewer.PageRedactor.EnableRedactionMode = true;
        }

        /// <summary>
        /// Remove the marked region to redact in the PDFViewer
        /// </summary>
        /// <param name="bounds">Region to be removed</param>
        private void RemoveMarkedRegionForRedaction(Dictionary<int, RectangleF> bounds)
        {
            if (redactRegions.ContainsKey(bounds.Keys.First()))
            {
                redactRegions[bounds.Keys.First()].Remove(bounds.Values.First());
                if (redactRegions[bounds.Keys.First()].Count == 0)
                {
                    redactRegions.Remove(bounds.Keys.First());
                    pdfViewer.PageRedactor.MarkRegions(bounds.Keys.First(), new List<RectangleF>());
                }
                else
                {
                    pdfViewer.PageRedactor.MarkRegions(bounds.Keys.First(), redactRegions[bounds.Keys.First()]);
                }
                pdfViewer.PageRedactor.EnableRedactionMode = true;
            }
        }

        /// <summary>
        /// Remove the prefix from the extracted sensitive information
        /// </summary>
        /// <param name="sensitiveInfo">List of sensitive Informations</param>
        /// <param name="selectedItems">List of selected options</param>
        /// <returns></returns>
        private List<string> RemovePrefix(List<string> sensitiveInfo, List<string> selectedItems)
        {
            for (int i = 0; i < sensitiveInfo.Count; i++)
            {
                foreach (var item in selectedItems)
                {
                    // Remove the selected items title prefix from the extracted sensitive information
                    string prefix = item + ": ";
                    if (sensitiveInfo[i].ToLower().Contains(prefix, StringComparison.Ordinal))
                    {
                        sensitiveInfo[i] = sensitiveInfo[i].Substring((sensitiveInfo[i].IndexOf(':') + 1));
                    }
                }
            }
            return sensitiveInfo;
        }

        /// <summary>
        /// Add the extracted sensitive information as a checkbox to the stack panel
        /// </summary>
        /// <param name="sensitiveInfo">Sensitive information</param>
        private void AddInformationToStack(List<string> sensitiveInfo)
        {
            //Index is set to be 3 to insert the checkboxes after the select all checkbox and the title textblock with total items count
            int infoIndex = 1;

            //Get the extracted sensitive information details from PDFViewer
            selectAll.Tag = sensitiveInfo;
            pdfViewer.FindText(sensitiveInfo, out Dictionary<int, List<TextSearchResult>> TextBounds);

            if (TextBounds != null && TextBounds.Count > 0)
            {
                //Add the extracted sensitive information as checkboxes to the stack panel
                for (int pageIndex = 0; pageIndex < pdfViewer.PageCount; pageIndex++)
                {
                    if (TextBounds.ContainsKey(pageIndex))
                    {
                        foreach (TextSearchResult result in TextBounds[pageIndex])
                        {
                            CheckBox checkBox = new CheckBox();
                            TextBlock contentText = new TextBlock();
                            contentText.Text = result.Text;
                            contentText.TextWrapping = TextWrapping.Wrap;
                            contentText.Width = 170;
                            checkBox.Content = contentText;
                            checkBox.Margin = new Thickness(25, 5, 0, 5);
                            checkBox.Tag = new Dictionary<int, RectangleF> { { pageIndex, result.Bounds } };
                            checkBox.Checked += CheckBox_Checked;
                            checkBox.Unchecked += CheckBox_Unchecked;
                            information_Stack.Children.Insert(infoIndex, checkBox);
                            infoIndex++;
                        }
                    }
                }
            }

            //Update the total count of the extracted sensitive information
            int totalInfoCount = information_Stack.Children.Count - 2;
            count_Label.Content = totalInfoCount + " informations found";
        }

        /// <summary>
        /// Method to collapse the AI assistance grid
        /// </summary>
        private void CollapseAIAssistance()
        {
            // Hide the AI assistance grid
            aiGrid.Visibility = Visibility.Collapsed;
            pdfViewer.Focus();

            if (option_Scroll.Visibility == Visibility.Visible || contents_Stack.Visibility == Visibility.Visible)
            {
                ResetRedactionOptions();
            }
            else
            {
                RemoveInformationFromStack();
                option_Scroll.Visibility = Visibility.Visible;
                contents_Stack.Visibility = Visibility.Visible;
                selectAll.Visibility = Visibility.Visible;
            }
        }

        private void ResetRedactionOptions()
        {
            foreach (UIElement redactOption in contents_Stack.Children)
            {
                CheckBox option = redactOption as CheckBox;
                if (option != null && (bool)option.IsChecked)
                {
                    option.IsChecked = false;
                }
            }
        }

        /// <summary>
        /// Method to remove the extracted sensitive information checkboxes from the stack panel
        /// </summary>
        private void RemoveInformationFromStack()
        {
            //Remove the extracted sensitive information from the stack panel
            while (information_Stack.Children.Count > 2)
            {
                information_Stack.Children.RemoveAt(1);
            }

            //Clear the marked regions to redact
            pdfViewer.PageRedactor.EnableRedactionMode = false;
            for (int pageIndex = 0; pageIndex < pdfViewer.PageCount; pageIndex++)
            {
                pdfViewer.PageRedactor.MarkRegions(pageIndex, new List<RectangleF>());
            }

            pdfViewer.PageRedactor.EnableRedactionMode = true;

            //Show the redact options checkboxes
            option_Scroll.Visibility = Visibility.Visible;
            information_Grid.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Method to scroll to the marked region in the PDFViewer
        /// </summary>
        /// <param name="pageIndex">Page index to be scrolled</param>
        /// <param name="bounds">Offset to be scrolled</param>
        private void ScrollToMarkedRegion(int pageIndex, RectangleF bounds)
        {
            double verticalOffset = 0;
            //Calculate the vertical offset of the marked region's page index
            for (int index = 0; index < pageIndex; index++)
            {
                var page = pdfViewer.LoadedDocument.Pages[index];
                PdfUnitConvertor m_unitConvertor = new PdfUnitConvertor();
                float pageHeight = m_unitConvertor.ConvertToPixels(page.Size.Height, PdfGraphicsUnit.Point);

                verticalOffset += pageHeight;

            }

            //Scroll to the calculated marked region in the PDFViewer
            pdfViewer.ScrollTo(bounds.X, verticalOffset + bounds.Y + 20);

        }
        #endregion

        #region OpenAI
        /// <summary>
        /// Method to extract the selected sensitive information from the extracted text using OpenAI
        /// </summary>
        /// <param name="text">Extracted text content</param>
        /// <param name="selectedItems">Select options to be extracted</param>
        /// <returns>Returns the list of sensitive information from the extracted text</returns>
        private async Task<List<string>> ExtractSelectedInfo(string text, List<string> selectedItems)
        {
            StringBuilder stringBuilder = new StringBuilder();
            // Create a prompt to extract the selected sensitive information
            stringBuilder.AppendLine("I have a block of text containing various pieces of information. Please help me identify and extract any Personally Identifiable Information (PII) present in the text. The PII categories I am interested in are:");
            // Add the selected sensitive information to the prompt
            foreach (var item in selectedItems)
            {
                stringBuilder.AppendLine(item);
            }
            // Add the instructions to the prompt
            stringBuilder.AppendLine("Please provide the extracted information as a plain list, separated by commas, without any prefix or numbering or extra content.");
            stringBuilder.AppendLine("While extracting names, Please extract names without including any titles such as 'Mr.,' 'Dr.,' or 'Mrs.' Simply provide the names without any prefixes.");
            string prompt = stringBuilder.ToString();

            // Call the OpenAI client to extract the selected sensitive information
            var completion = await microsoftAIExtension.GetAnswerFromGPT(prompt, text);
            string result = completion.ToString();

            // Process the output to ensure it only contains names
            var sensitiveInfo = result?.Split(new[] { '\n', ',' }, System.StringSplitOptions.RemoveEmptyEntries)
                             .Select(name => name.Trim())
                             .Where(name => !string.IsNullOrEmpty(name))
                             .ToList() ?? new List<string>();
            return sensitiveInfo;
        }
        #endregion
    }
}