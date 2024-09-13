using System.Windows;
using System.Windows.Controls;
using Syncfusion.Windows.Shared;

namespace Smart_Summarize
{
    internal class AIResultChatBox : Border
    {
        static AIResultChatBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AIResultChatBox), new FrameworkPropertyMetadata(typeof(AIResultChatBox)));
        }


        public void ApplyStyle()
        {
            this.HorizontalAlignment = HorizontalAlignment.Left;
            this.MaxWidth = 300;
            this.CornerRadius = new CornerRadius(5);
            this.BorderThickness = new Thickness(2);
            this.Margin = new Thickness(10);
            this.SetResourceReference(Button.BackgroundProperty, "SecondaryBackground");
            this.SetResourceReference(Button.ForegroundProperty, "SecondaryForeground");
        }
    }
}
