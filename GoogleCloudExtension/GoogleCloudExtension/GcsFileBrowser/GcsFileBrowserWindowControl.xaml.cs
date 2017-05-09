// Copyright 2017 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using GoogleCloudExtension.Utils;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GoogleCloudExtension.GcsFileBrowser
{
    /// <summary>
    /// Interaction logic for GcsFileBrowserWindowControl.
    /// </summary>
    public partial class GcsFileBrowserWindowControl : UserControl
    {
        private GcsBrowserViewModel ViewModel => (GcsBrowserViewModel)DataContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="GcsFileBrowserWindowControl"/> class.
        /// </summary>
        public GcsFileBrowserWindowControl()
        {
            this.InitializeComponent();
        }

        private void UserControl_Drop(object sender, DragEventArgs e)
        {
            ErrorHandlerUtils.HandleExceptions(() =>
            {
                if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    return;
                }

                var files = (string[])e.Data.GetData(DataFormats.FileDrop, autoConvert: false);
                ViewModel.StartDroppedFilesUpload(files);

                e.Handled = true;
            });
        }

        private void UserControl_DragOver(object sender, DragEventArgs e)
        {
            ErrorHandlerUtils.HandleExceptions(() =>
            {
                e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            });
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ErrorHandlerUtils.HandleExceptions(() =>
            {
                var self = (DataGrid)e.Source;
                ViewModel.InvalidateSelectedItems(self.SelectedItems.Cast<GcsRow>());
            });
        }
    }
}