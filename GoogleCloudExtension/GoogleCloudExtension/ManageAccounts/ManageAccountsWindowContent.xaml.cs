using System.Windows.Controls;
using System.Windows.Input;

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
