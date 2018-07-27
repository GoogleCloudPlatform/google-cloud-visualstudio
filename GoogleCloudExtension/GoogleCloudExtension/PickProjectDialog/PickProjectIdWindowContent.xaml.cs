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

using GoogleCloudExtension.Theming;
using System.Windows.Controls;
using System.Windows.Data;

namespace GoogleCloudExtension.PickProjectDialog
{
    /// <summary>
    /// Interaction logic for PickProjectIdWindowContent.xaml
    /// </summary>
    public partial class PickProjectIdWindowContent : CommonWindowContent<IPickProjectIdViewModel>
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public const string CvsKey = "cvs";

        public PickProjectIdWindowContent(string helpText, bool allowAccountChange) : this(
            new PickProjectIdViewModel(helpText, allowAccountChange))
        { }

        public PickProjectIdWindowContent(IPickProjectIdViewModel viewModel) : base(
            viewModel,
            GoogleCloudExtension.Resources.PublishDialogSelectGcpProjectTitle)
        {
            InitializeComponent();

            DataContext = ViewModel;

            // Ensure the focus is in the filter textbox.
            _filter.Focus();
        }

        private void OnFilterItemInCollectionView(object sender, FilterEventArgs e) =>
            e.Accepted = ViewModel?.FilterItem(e.Item) ?? false;

        private void OnFilterTextChanged(object sender, TextChangedEventArgs e)
        {
            var collectionViewSource = Resources[CvsKey] as CollectionViewSource;
            collectionViewSource?.View?.Refresh();
        }
    }
}
