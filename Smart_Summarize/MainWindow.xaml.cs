using Syncfusion.Windows.PdfViewer;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Syncfusion.Windows.Shared;
using System.IO;
using Syncfusion.UI.Xaml.Chat;

namespace Smart_Summarize
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields
        private ToggleButton aIAssistButton;
        private AIAssitViewModel viewModel;
        #endregion

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
            //Load the PDF document in the PdfViewer
            pdfViewer.Load("../../../Data/GIS Succinctly.pdf");
            viewModel = new AIAssitViewModel(pdfViewer);
            DataContext = viewModel;
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
            aiAssistView.Visibility = Visibility.Collapsed;

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
        /// <summary>
        /// Handles the Unchecked event of the AIAssistButton.
        /// </summary>
        /// <param name="sender">The Button that triggered the event.</param>
        /// <param name="e">The event data.</param>
        private void AIAssistButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = sender as ToggleButton;
            if (toggleButton != null)
            {
                (toggleButton.Content as System.Windows.Shapes.Path).SetResourceReference(System.Windows.Shapes.Path.FillProperty, "SecondaryForeground");
            }
            aiAssistView.Visibility = Visibility.Collapsed;
            summarizeGrid.Visibility = Visibility.Collapsed;
            pdfViewer.Focus();
        }
        /// <summary>
        /// Handles the checked event of the AIAssistButton.
        /// </summary>
        /// <param name="sender">The Button that triggered the event.</param>
        /// <param name="e">The event data.</param>
        private async void AIAssistButton_Checked(object sender, RoutedEventArgs e)
        {
            summarizeGrid.Visibility = Visibility.Visible;
            aiAssistView.Visibility = Visibility.Visible;
            if (viewModel.Chats.Count == 0)
            {
                // Ensure this task runs asynchronously
                await viewModel.GenerateMessages();
            }
        }

        #endregion
        /// <summary>
        /// Handles the event when a suggestion is selected in the chat.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data containing the selected suggestion.</param>
        private void chat_SuggestionSelected(object sender, SuggestionClickedEventArgs e)
        {
            var msgs = aiAssistView.DataContext as AIAssitViewModel;
            msgs.Chats.Add(new TextMessage { Text = e.Item.ToString(), DateTime = DateTime.Now, Author = aiAssistView.CurrentUser });
        }
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
            aIAssistButton = new ToggleButton();
            //create path for the AI Assist button
            System.Windows.Shapes.Path aIAssistPath= new System.Windows.Shapes.Path();
            aIAssistPath.Data = PathMarkupToGeometry("M12.7393 0.396992C12.6915 0.170185 12.4942 0.00592726 12.2624 0.000154793C12.0307 -0.00561768 11.8254 0.148608 11.7665 0.372757L11.6317 0.884708C11.4712 1.49476 10.9948 1.97121 10.3847 2.13174L9.87275 2.26646C9.66167 2.32201 9.51101 2.50807 9.50057 2.72609C9.49013 2.94411 9.62233 3.14371 9.82714 3.21917L10.5469 3.48434C11.0663 3.67571 11.4646 4.10157 11.6208 4.63265L11.7703 5.14108C11.8343 5.35876 12.0369 5.50604 12.2637 5.49981C12.4905 5.49358 12.6847 5.33539 12.7367 5.11452L12.8292 4.72158C12.9661 4.1398 13.3904 3.66811 13.9545 3.47066L14.6652 3.22193C14.8737 3.14895 15.0096 2.94777 14.9995 2.72708C14.9893 2.50639 14.8356 2.31851 14.6213 2.26493L14.1122 2.13768C13.4623 1.9752 12.9622 1.45598 12.8242 0.800451L12.7393 0.396992ZM11.3796 2.78214C11.7234 2.57072 12.0165 2.28608 12.2378 1.94927C12.458 2.28452 12.7496 2.5685 13.0919 2.77995C12.7482 2.99134 12.4563 3.27526 12.2359 3.60987C12.015 3.27569 11.7229 2.99268 11.3796 2.78214ZM5.30942 1.83391C5.52745 1.82347 5.72704 1.95566 5.8025 2.16048L6.27391 3.44001C6.65666 4.47889 7.50838 5.27542 8.57054 5.58781L9.47441 5.85365C9.68272 5.91492 9.82768 6.10362 9.83317 6.32068C9.83867 6.53773 9.70345 6.73353 9.49851 6.80526L8.23512 7.24746C7.10689 7.64235 6.25821 8.58572 5.98442 9.74929L5.82004 10.4479C5.76807 10.6687 5.57388 10.8269 5.34707 10.8331C5.12025 10.8394 4.91767 10.6921 4.85365 10.4744L4.58781 9.57055C4.27542 8.50839 3.47889 7.65666 2.44001 7.27391L1.16048 6.8025C0.955663 6.72705 0.823467 6.52745 0.833905 6.30942C0.844343 6.0914 0.995002 5.90534 1.20609 5.8498L2.11623 5.61029C3.33634 5.28922 4.28922 4.33634 4.61029 3.11623L4.84979 2.20609C4.90534 1.995 5.0914 1.84434 5.30942 1.83391ZM5.39094 3.92847C4.93428 5.04593 4.04593 5.93429 2.92847 6.39094C3.985 6.82147 4.83251 7.63474 5.30901 8.65751C5.79435 7.61107 6.66906 6.78063 7.76225 6.35619C6.69032 5.88974 5.83607 5.02082 5.39094 3.92847ZM11.5124 7.00016C11.7442 7.00593 11.9415 7.17019 11.9893 7.39699L12.1025 7.93494C12.2997 8.87141 13.0141 9.61316 13.9426 9.84526L14.6213 10.0149C14.8356 10.0685 14.9893 10.2564 14.9995 10.4771C15.0096 10.6978 14.8737 10.8989 14.6652 10.9719L13.7176 11.3036C12.9117 11.5856 12.3056 12.2595 12.11 13.0906L11.9867 13.6145C11.9347 13.8354 11.7405 13.9936 11.5137 13.9998C11.2869 14.006 11.0843 13.8588 11.0203 13.6411L10.8209 12.9632C10.5978 12.2045 10.0288 11.5961 9.28679 11.3227L8.32714 10.9692C8.12233 10.8937 7.99013 10.6941 8.00057 10.4761C8.01101 10.2581 8.16167 10.072 8.37275 10.0165L9.05536 9.83684C9.92686 9.6075 10.6075 8.92687 10.8368 8.05536L11.0165 7.37276C11.0754 7.14861 11.2807 6.99438 11.5124 7.00016ZM11.4838 9.10985C11.1444 9.72499 10.6264 10.2254 9.9977 10.543C10.6262 10.8597 11.1422 11.3576 11.4815 11.9678C11.8194 11.3576 12.3344 10.8584 12.9632 10.5402C12.3367 10.2219 11.8215 9.72215 11.4838 9.10985Z");
            aIAssistPath.Fill = textSearchButton.Foreground;
            aIAssistPath.Height = 14;
            aIAssistPath.Width = 17;
            aIAssistButton.Content = aIAssistPath;
            aIAssistButton.Checked += AIAssistButton_Checked;
            aIAssistButton.Unchecked += AIAssistButton_Unchecked;
            aIAssistButton.Height = 32;
            aIAssistButton.Margin = new Thickness(0, 0, 8, 0);
            aIAssistButton.Padding = new Thickness(6,0,6,0);
            // Set the style of the AI Assist button
            aIAssistButton.SetResourceReference(ToggleButton.StyleProperty, "WPFToggleButtonStyle");

            // Add AI Assist button to the text search stack of the toolbar
            if (textSeacrchStack.Children != null && textSeacrchStack.Children.Count > 0)
            {
                textSeacrchStack.Children.Insert(0, aIAssistButton);
            }
            else
            {
                textSeacrchStack.Children.Add(aIAssistButton);
            }
        }
        #endregion

        #region Path Geometry Helper Methods
        /// <summary>
        /// Converts a path markup string into a Geometry object.
        /// </summary>
        /// <param name="pathMarkup">The path markup string defining the geometry.</param>
        /// <returns>A Geometry object created from the given path markup.</returns>
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
        /// <summary>
        /// Generates a Stream from the given string.
        /// </summary>
        /// <param name="s">The input string to convert into a stream.</param>
        /// <returns>A Stream containing the string data.</returns>
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
    } 

}

