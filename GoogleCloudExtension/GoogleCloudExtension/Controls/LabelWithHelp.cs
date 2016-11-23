using System.Windows;

namespace GoogleCloudExtension.Controls
{
    /// <summary>
    /// This control represents a label with a help anchor to show help messages to the user.
    /// </summary>
    public class LabelWithHelp : System.Windows.Controls.Label
    {
        public static readonly DependencyProperty HelpContentProperty =
            DependencyProperty.Register(
                nameof(HelpContent),
                typeof(object),
                typeof(LabelWithHelp));

        /// <summary>
        /// This property contains the help content to be shown when hovering over the help anchor.
        /// </summary>
        public object HelpContent
        {
            get { return GetValue(HelpContentProperty); }
            set { SetValue(HelpContentProperty, value); }
        }
    }
}
