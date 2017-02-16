//------------------------------------------------------------------------------
// <copyright file="ErrorReportingDetailToolWindowControl.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    using GoogleCloudExtension.Accounts;
    using GoogleCloudExtension.Utils;
    using System;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;

    /// <summary>
    /// Interaction logic for ErrorReportingDetailToolWindowControl.
    /// </summary>
    public partial class ErrorReportingDetailToolWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorReportingDetailToolWindowControl"/> class.
        /// </summary>
        public ErrorReportingDetailToolWindowControl()
        {
            this.InitializeComponent();
            ViewModel = new ErrorReportingDetailViewModel();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            DataContext = ViewModel;
            autoReloadToggleButton.AutoReload += (sender, e) => ViewModel.UpdateGroupAndEventAsync();
        }

        public ErrorReportingDetailViewModel ViewModel { get; }

        /// <summary>
        /// Get the first ancestor control element of type TControl.
        /// </summary>
        /// <typeparam name="TUIElement">A <seealso cref="UIElement"/> type.</typeparam>
        /// <param name="obj">A <seealso cref="DependencyObject"/> element. </param>
        /// <returns>null or TControl object.</returns>
        private TUIElement FindAncestorControl<TUIElement>(DependencyObject obj) where TUIElement : UIElement
        {
            while ((obj != null) && !(obj is TUIElement))
            {
                obj = VisualTreeHelper.GetParent(obj);
            }

            return obj as TUIElement;  // Note, "null as TUIElement" is valid and returns null. 
        }

        /// <summary>
        /// When mouse click on a row, toggle display the row detail.
        /// if the mouse is clikcing on detail panel, does not collapse it.
        /// </summary>
        private void dataGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = FindAncestorControl<DataGridRow>(e.OriginalSource as DependencyObject);
            if (row != null)
            {
                if (null != FindAncestorControl<DataGridDetailsPresenter>(e.OriginalSource as DependencyObject))
                {
                    return;
                }

                row.DetailsVisibility =
                    row.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void LinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (ErrorReportingDetailToolWindowCommand.Instance == null)
            {
                MessageBox.Show("ErrorReportingDetailToolWindowCommand.Instance == null");
            }
            else
            {
                ErrorReportingToolWindowCommand.Instance.ShowToolWindow(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// This is to enable outer scroll bar.
        /// </summary>
        private void dataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent.RaiseEvent(eventArg);
            }
        }
    }
}