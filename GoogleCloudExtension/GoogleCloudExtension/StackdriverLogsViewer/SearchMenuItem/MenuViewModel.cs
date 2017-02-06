using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    public class MenuItemViewModel : Model, IMenuItem
    {
        public IMenuItem MenuItemParent { get; }

        public string Header { get; set; }

        public ObservableCollection<IMenuItem> MenuItems { get; }

        public ProtectedCommand MenuCommand { get; }

        public MenuItemViewModel(IMenuItem parent)
        {
            MenuItemParent = parent;
            MenuCommand = new ProtectedCommand(Execute);
            MenuItems = new ObservableCollection<IMenuItem>();
        }

        public void MenuCommandBubblingCallback(IMenuItem caller)
        {
            MenuItemParent.MenuCommandBubblingCallback(caller);
        }

        protected virtual void Execute()
        {
            // (NOTE: In a view model, you normally should not use MessageBox.Show()).
            // MessageBox.Show("Clicked at " + Header);
            MenuItemParent.MenuCommandBubblingCallback(this);
        }
    }
}
