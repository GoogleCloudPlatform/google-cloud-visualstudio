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

using System.ComponentModel.Composition;
using System.Windows.Controls;
using GoogleCloudExtension.TeamExplorerExtension;
using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.CloudSourceRepositories
{
    /// <summary>
    /// Interaction logic for CsrSectionControl.xaml
    /// Export ISectionView that MEF will create instances of the class.
    /// </summary>
    [Export(typeof(ISectionView))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class CsrSectionControl : UserControl, ISectionView
    {
        /// <summary>
        /// Implement <seealso cref="ISectionView.ViewModel"/>
        /// </summary>
        public ISectionViewModel ViewModel { get; }

        /// <summary>
        /// Implement <seealso cref="ISectionView.Title"/>
        /// </summary>
        string ISectionView.Title { get; } = GoogleCloudExtension.Resources.CsrConnectSectionTitle;

        /// <summary>
        /// Add <seealso cref="ImportingConstructorAttribute"/>.
        /// This is to tell MEF to use this as default constructor instead of
        /// the parameterless one.
        /// </summary>
        /// <param name="viewModel">
        /// MEF will create a <seealso cref="ISectionViewModel"/> object
        /// and pass in here to create an instance of <seealso cref="ISectionView"/> object.
        /// </param>
        [ImportingConstructor]
        public CsrSectionControl(ISectionViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel.ThrowIfNull(nameof(viewModel));
            DataContext = viewModel;
        }
    }
}