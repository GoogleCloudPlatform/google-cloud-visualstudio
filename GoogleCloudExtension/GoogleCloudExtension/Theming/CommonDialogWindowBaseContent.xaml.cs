// Copyright 2016 Google Inc. All Rights Reserved.
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

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace GoogleCloudExtension.Theming
{
    /// <summary>
    /// Interaction logic for PromptDialogBaseContent.xaml
    /// 
    /// This class defines the common visuals for all of the dialogs in the extension. In order to use it you need to
    /// set the property <seealso cref="DialogContent"/> to the element that should be displayed in the main portion of the
    /// dialog. The <seealso cref="Buttons"/> property should set to the list of buttons to display in the dialog.
    /// Because the <seealso cref="Buttons"/> property **does not** allow the DataContext to flow through, any bindings in the
    /// <seealso cref="DialogButtonInfo"/> instances will have use an explicit source.
    /// </summary>
    public partial class CommonDialogWindowBaseContent : UserControl
    {
        public static readonly DependencyProperty DialogContentProperty =
            DependencyProperty.Register(
                nameof(DialogContent),
                typeof(object),
                typeof(CommonDialogWindowBaseContent),
                new PropertyMetadata(OnDialogContentPropertyChanged));

        /// <summary>
        /// The main content of the dialog.
        /// </summary>
        public object DialogContent
        {
            get { return GetValue(DialogContentProperty); }
            set { SetValue(DialogContentProperty, value); }
        }

        /// <summary>
        /// The list of buttons to show in the dialog.
        /// </summary>
        public ObservableCollection<DialogButtonInfo> Buttons { get; }

        public CommonDialogWindowBaseContent()
        {
            InitializeComponent();

            Buttons = new ObservableCollection<DialogButtonInfo>();

            _buttonsRow.ItemsSource = Buttons;
        }

        private static void OnDialogContentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (CommonDialogWindowBaseContent)d;
            self._content.Content = e.NewValue;
        }
    }
}
