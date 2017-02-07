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

using GoogleCloudExtension.Utils;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    public class MenuItemViewModel : ViewModelBase, IMenuItem
    {
        private bool _isSubmenuPopulated = false;

        public static readonly MenuItemViewModel FakeItem = new MenuItemViewModel(null) { IsFakeItem = true };

        public bool IsFakeItem { get; private set; }

        public IMenuItem MenuItemParent { get; }

        public string Header { get; set; }

        public ObservableCollection<IMenuItem> MenuItems { get; }

        public ProtectedCommand MenuCommand { get; }

        public ProtectedCommand OnSubmenuOpenCommand { get; }

        public bool IsSubmenuPopulated
        {
            get { return _isSubmenuPopulated; }
            set { SetValueAndRaise(ref _isSubmenuPopulated, value); }
        }

        public MenuItemViewModel(IMenuItem parent)
        {
            IsFakeItem = false;
            MenuItemParent = parent;
            MenuCommand = new ProtectedCommand(Execute);
            MenuItems = new ObservableCollection<IMenuItem>();
            OnSubmenuOpenCommand = new ProtectedCommand(() => AddItems());
        }

        public void MenuCommandBubblingCallback(IMenuItem caller)
        {
            MenuItemParent?.MenuCommandBubblingCallback(caller);
        }

        protected virtual async Task LoadSubMenu() 
        {
            return;
        }

        protected virtual void Execute()
        {
            MenuItemParent?.MenuCommandBubblingCallback(this);
        }

        private async Task AddItems()
        {
            if (IsSubmenuPopulated || Loading)
            {
                return;
            }

            Loading = true;
            Debug.WriteLine($"{Header} call AddItems from viewModel.");
            await LoadSubMenu();
            IsSubmenuPopulated = true;
            Loading = false;
        }
    }
}
