//------------------------------------------------------------------------------
// <copyright file="GcsFileBrowserWindowControl.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace GoogleCloudExtension.GcsFileBrowser
{
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for GcsFileBrowserWindowControl.
    /// </summary>
    public partial class GcsFileBrowserWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GcsFileBrowserWindowControl"/> class.
        /// </summary>
        public GcsFileBrowserWindowControl()
        {
            this.InitializeComponent();
        }

        private void UserControl_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            var files = (string[])e.Data.GetData(DataFormats.FileDrop, autoConvert: false);
            var viewModel = (GcsBrowserViewModel)DataContext;
            viewModel.StartFileUpload(files);

            e.Handled = true;
        }

        private void UserControl_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;  
        }
    }
}