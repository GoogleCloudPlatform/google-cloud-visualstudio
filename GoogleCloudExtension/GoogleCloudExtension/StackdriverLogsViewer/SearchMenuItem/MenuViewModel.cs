using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    public interface IMenuItem 
    {
        string Header { get; set; }

        ObservableCollection<IMenuItem> MenuItems { get; }

        ProtectedCommand MenuCommand { get; }

        IMenuItem MenuItemParent { get; }

        void MenuCommandBubblingCallback(IMenuItem caller);
    }

    public class MenuItemViewModel : ViewModelBase, IMenuItem
    {
        public bool IsFakeItem { get; private set; }

        public IMenuItem MenuItemParent { get; }

        public string Header { get; set; }

        public ObservableCollection<IMenuItem> MenuItems { get; }

        public ProtectedCommand MenuCommand { get; }
        public ProtectedCommand OnSubmenuOpenCommand { get; }


        private bool _isSubmenuPopulated = false;
        public bool IsSubmenuPopulated
        {
            get { return _isSubmenuPopulated; }
            set { SetValueAndRaise(ref _isSubmenuPopulated, value); }
        }

        public static MenuItemViewModel CreateFakeItem()
        {
            return new MenuItemViewModel(null) { IsFakeItem = true };
        }

        public MenuItemViewModel(IMenuItem parent)
        {
            IsFakeItem = false;
            MenuItemParent = parent;
            MenuCommand = new ProtectedCommand(Execute);
            MenuItems = new ObservableCollection<IMenuItem>();
            OnSubmenuOpenCommand = new ProtectedCommand(() => AddItems());
        }

        private async Task AddItems()
        {
            if (IsSubmenuPopulated || Loading)
            {
                return;
            }

            Loading = true;
            Debug.WriteLine($"{Header} AddItems from viewModel");
            await LoadSubMenu();
            Debug.WriteLine($"{Header} Set OnInit false from  AddItems from viewModel");
            IsSubmenuPopulated = true;
            Loading = false;
        }

        protected virtual async Task LoadSubMenu()
        {
            return;
        }

        public void MenuCommandBubblingCallback(IMenuItem caller)
        {
            MenuItemParent?.MenuCommandBubblingCallback(caller);
        }

        protected virtual void Execute()
        {
            // (NOTE: In a view model, you normally should not use MessageBox.Show()).
            // MessageBox.Show("Clicked at " + Header);
            MenuItemParent?.MenuCommandBubblingCallback(this);
        }
    }
}
