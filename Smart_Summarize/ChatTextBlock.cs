using System.Windows;
using System.Windows.Controls;

namespace Smart_Summarize
{
    internal class ChatTextBlock : TextBlock
    {
        static ChatTextBlock()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ChatTextBlock), new FrameworkPropertyMetadata(typeof(ChatTextBlock)));
        }

        public void ApplyStyle()
        {
            this.MaxWidth = 300;
            this.TextWrapping = TextWrapping.Wrap;
            this.Margin = new Thickness(5);
            this.FontSize = 12;
        }
    }
}
