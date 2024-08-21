using System.Windows;
using System.Windows.Controls;
using Syncfusion.Windows.Shared;

namespace Smart_Summarize
{
    internal class UserChatBox : Border
    {
        static UserChatBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(UserChatBox), new FrameworkPropertyMetadata(typeof(UserChatBox)));
        }


        public void Applystyle(string skinManagerStyle)
        {
            this.HorizontalAlignment = HorizontalAlignment.Right;
            this.MaxWidth = 300;
            this.CornerRadius = new CornerRadius(5);
            this.BorderThickness = new Thickness(2);
            this.Margin = new Thickness(10);
            if (skinManagerStyle != null && !string.IsNullOrEmpty(skinManagerStyle.ToString()) && skinManagerStyle.ToString().ToLower().Contains("windows11"))
            {
                this.SetResourceReference(Button.BackgroundProperty, "ContentBackgroundHovered");
                this.SetResourceReference(Button.ForegroundProperty, "IconColorHovered");
            }
            else
            {
                this.SetResourceReference(Button.BackgroundProperty, "SecondaryBackgroundHovered");
                this.SetResourceReference(Button.ForegroundProperty, "SecondaryForegroundHovered");
            }
        }
    }
}
