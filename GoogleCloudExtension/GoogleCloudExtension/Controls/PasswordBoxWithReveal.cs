using System.Windows;
using System.Windows.Controls;

namespace GoogleCloudExtension.Controls
{
    public class PasswordBoxWithReveal : Control
    {
        // Part names for the control.
        const string PasswordBoxPart = "PART_PasswordBox";
        const string TextBoxPart = "PART_TextBox";
        const string RevealPart = "PART_Reveal";

        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register(
                nameof(Password),
                typeof(string),
                typeof(PasswordBoxWithReveal),
                new PropertyMetadata { PropertyChangedCallback = OnPasswordPropertyChanged });

        public static readonly DependencyProperty ShowingPasswordProperty =
            DependencyProperty.Register(
                nameof(ShowingPassword),
                typeof(bool),
                typeof(PasswordBoxWithReveal),
                new PropertyMetadata { DefaultValue = false });
                
        private Button _reveal;
        private TextBox _textBox;
        private PasswordBox _passwordBox;

        public string Password
        {
            get { return (string)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }

        public bool ShowingPassword
        {
            get { return (bool)GetValue(ShowingPasswordProperty); }
            set { SetValue(ShowingPasswordProperty, value); }
        }

        public override void OnApplyTemplate()
        {
            _reveal = GetTemplateChild(RevealPart) as Button;
            _textBox = GetTemplateChild(TextBoxPart) as TextBox;
            _passwordBox = GetTemplateChild(PasswordBoxPart) as PasswordBox;

            if (_textBox != null)
            {
                _textBox.IsReadOnly = true;
            }
            if (_passwordBox != null)
            {
                _passwordBox.IsEnabled = false;

                // Since the PasswordBox.Password property is not a DP we cannot use TemplateBinding
                // or any other form of binding to set the value, we must do it manually.
                _passwordBox.Password = Password;
            }

            if (_reveal != null)
            {
                _reveal.Click += OnRevealClicked;
            }
        }

        private void OnRevealClicked(object sender, RoutedEventArgs e)
        {
            ShowingPassword = !ShowingPassword;
        }

        private void UpdatePassword(string newValue)
        {
            if (_passwordBox != null)
            {
                _passwordBox.Password = newValue;
            }
        }

        private static void OnPasswordPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var self = (PasswordBoxWithReveal)sender;
            self.UpdatePassword((string)e.NewValue);
        }
    }
}
