using Syncfusion.Windows.PdfViewer;
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

namespace Smart_Summarize
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
        }
        #endregion

        #region Event Handlers
        private void pdfViewer_DocumentLoaded(object sender, EventArgs args)
        {
            //Collapse the AI Assistance when the document is loaded
            if (aIAssistButton != null && aIAssistButton.IsChecked.Value)
            {
                aIAssistButton.IsChecked = false;
            }
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
        private void AIAssistButton_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void AIAssistButton_Checked(object sender, RoutedEventArgs e)
        {

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
            aIAssistText.Text = "AI Summarizer";
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
        }
        #endregion
    }
}