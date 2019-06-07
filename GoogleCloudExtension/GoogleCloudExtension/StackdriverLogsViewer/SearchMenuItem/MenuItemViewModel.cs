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

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.StackdriverLogsViewer.SearchMenuItem
{
    /// <summary>
    /// View model for SearchMenuItem control.
    /// </summary>
    public class MenuItemViewModel : ViewModelBase
    {
        private bool _isSubmenuPopulated = true;
        private string _header;

        /// <summary>
        /// An invisible menu item.
        /// </summary>
        public static readonly MenuItemViewModel InvisibleItem = new MenuItemViewModel(null) { IsVisible = false };

        /// <summary>
        /// Gets or sets if the menu item is visible.
        /// </summary>
        public bool IsVisible { get; private set; }

        /// <summary>
        /// Gets the parent menu item view model.
        /// </summary>
        public MenuItemViewModel MenuItemParent { get; }

        /// <summary>
        /// The menu item header.
        /// </summary>
        public string Header
        {
            get { return _header; }
            set { SetValueAndRaise(ref _header, value); }
        }

        /// <summary>
        /// Submenu items view model collection.
        /// </summary>
        public ObservableCollection<MenuItemViewModel> MenuItems { get; }

        /// <summary>
        /// Menu selected command.
        /// </summary>
        public ProtectedCommand MenuCommand { get; }

        /// <summary>
        /// Submenu open event handler.
        /// </summary>
        public ProtectedAsyncCommand OnSubmenuOpenCommand { get; }

        /// <summary>
        /// Indicate if the submenu list is populated.
        /// Set to true disables loading submenu items.
        /// False: It loads submenu items when the popup menu item opens.
        /// Default is true.
        /// </summary>
        public bool IsSubmenuPopulated
        {
            get { return _isSubmenuPopulated; }
            set { SetValueAndRaise(ref _isSubmenuPopulated, value); }
        }

        /// <summary>
        /// Initializes an instance of <seealso cref="MenuItemViewModel"/> class.
        /// </summary>
        /// <param name="parent">The parent menu item view model.</param>
        public MenuItemViewModel(MenuItemViewModel parent)
        {
            IsVisible = true;
            MenuItemParent = parent;
            MenuCommand = new ProtectedCommand(() => CommandBubblingHandler(this));
            MenuItems = new ObservableCollection<MenuItemViewModel>();
            OnSubmenuOpenCommand = new ProtectedAsyncCommand(AddItemsAsync);
        }

        /// <summary>
        /// Child menu item calls parent's bubbling handler when it is clicked.
        /// </summary>
        /// <param name="originalSource">The original menu item that fires the selected event.</param>
        protected virtual void CommandBubblingHandler(MenuItemViewModel originalSource)
            => MenuItemParent?.CommandBubblingHandler(originalSource);

        /// <summary>
        /// Inherited classes implement this to perform delay loading of sub menu items.
        /// </summary>
        protected virtual Task LoadSubMenuAsync()
        {
            return Task.FromResult(0);
        }

        private async Task AddItemsAsync()
        {
            if (IsSubmenuPopulated || Loading)
            {
                return;
            }

            Loading = true;
            Debug.WriteLine($"{Header} call AddItems from viewModel.");
            await LoadSubMenuAsync();
            IsSubmenuPopulated = true;
            Loading = false;
        }
    }
}
