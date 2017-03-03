using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace GoogleCloudExtension.Controls
{
    public class AutoReloadButton : ImageToggleButton
    {
        private readonly DispatcherTimer _timer;

        static AutoReloadButton()
        {
            ToggleButton.IsCheckedProperty.OverrideMetadata(
                typeof(AutoReloadButton), 
                new PropertyMetadata(false, OnIsCheckedChanged));
        }

        private static void OnIsCheckedChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            bool isChecked = (bool)e.NewValue;
            AutoReloadButton button = source as AutoReloadButton;
            if (isChecked)
            {
                button._timer.Start();
            }
            else
            {
                button._timer.Stop();
            }
        }

        public static DependencyProperty AutoReloadCommandProperty =
            DependencyProperty.Register(
                nameof(AutoReloadCommand),
                typeof(ICommand),
                typeof(AutoReloadButton));

        public ICommand AutoReloadCommand
        {
            get { return (ICommand)GetValue(AutoReloadCommandProperty); }
            set { SetValue(AutoReloadCommandProperty, value); }
        }

        public AutoReloadButton()
        {
            _timer = new DispatcherTimer();
        }

        /// <summary>
        /// Initialize the control template.
        /// </summary>
        public override void OnApplyTemplate()
        {
            //  DispatcherTimer setup
            _timer.Interval = TimeSpan.FromSeconds(10);
            _timer.Tick += (sender, e) =>
            {
                if (AutoReloadCommand == null || !AutoReloadCommand.CanExecute(null))
                {
                    return;
                }

                AutoReloadCommand.Execute(null);
            };
        }
    }
}
