using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoogleCloudExtension.Controls
{
    /// <summary>
    /// This class represents a specialization of the button control that only shows icons, showing
    /// different icons depending on the visual state.
    /// </summary>
    public class IconButton : Button
    {
        public static readonly DependencyProperty NormalIconProperty =
            DependencyProperty.Register(
                nameof(NormalIcon),
                typeof(ImageSource),
                typeof(IconButton));

        public static readonly DependencyProperty MouseOverIconProperty =
            DependencyProperty.Register(
                nameof(MouseOverIcon),
                typeof(ImageSource),
                typeof(IconButton));

        /// <summary>
        /// The icon to show in the normal state.
        /// </summary>
        public ImageSource NormalIcon
        {
            get { return (ImageSource)GetValue(NormalIconProperty); }
            set { SetValue(NormalIconProperty, value); }
        }

        /// <summary>
        /// The icon to show in the mouse over state.
        /// </summary>
        public ImageSource MouseOverIcon
        {
            get { return (ImageSource)GetValue(MouseOverIconProperty); }
            set { SetValue(MouseOverIconProperty, value); }
        }
    }
}
