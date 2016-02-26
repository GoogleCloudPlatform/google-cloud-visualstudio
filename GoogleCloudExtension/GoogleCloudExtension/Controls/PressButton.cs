using System.Windows;
using System.Windows.Controls;

namespace GoogleCloudExtension.Controls
{
    public class PressButton : Button
    {
        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
            nameof(IsChecked), typeof(bool), typeof(PressButton));
        
        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }
    }
}
