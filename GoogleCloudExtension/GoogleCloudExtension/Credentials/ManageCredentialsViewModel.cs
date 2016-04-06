using GoogleCloudExtension.Credentials.Models;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Credentials
{
    public class ManageCredentialsViewModel : ViewModelBase
    {
        private AsyncPropertyValue<IEnumerable<UserCredentialsViewModel>> _userCredentialsListAsync;
        private string _currentAccountName;

        public AsyncPropertyValue<IEnumerable<UserCredentialsViewModel>> UserCredentialsListAsync
        {
            get { return _userCredentialsListAsync; }
            set { SetValueAndRaise(ref _userCredentialsListAsync, value); }
        }

        public string CurrentAccountName
        {
            get { return _currentAccountName; }
            set { SetValueAndRaise(ref _currentAccountName, value); }
        }

        public ManageCredentialsViewModel()
        {
            _userCredentialsListAsync = new AsyncPropertyValue<IEnumerable<UserCredentialsViewModel>>(LoadUserCredentialsViewModel());
        }

        private async Task<IEnumerable<UserCredentialsViewModel>> LoadUserCredentialsViewModel()
        {
            var userCredentials = await CredentialsManager.GetCredentialsListAsync();
            var result = userCredentials.Select(x => new UserCredentialsViewModel(x)).ToList();
            return result;
        }
    }
}
