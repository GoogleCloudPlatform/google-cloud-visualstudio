using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GoogleCloudExtension.ManageAccounts
{
    /// <summary>
    /// Interaction logic for ManageCredentialsWindowContent.xaml
    /// </summary>
    public partial class ManageAccountsWindowContent : UserControl
    {
        public ManageAccountsWindowContent()
        {
            InitializeComponent();
        }

        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_accountsListBox.SelectedItem != null)
            {
                var viewModel = (ManageAccountsViewModel)DataContext;
                viewModel.DoucleClickedItem((UserAccountViewModel)_accountsListBox.SelectedItem);
            }
        }
    }
}
