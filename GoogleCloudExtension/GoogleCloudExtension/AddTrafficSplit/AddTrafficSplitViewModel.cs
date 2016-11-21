using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoogleCloudExtension.AddTrafficSplit
{
    public class AddTrafficSplitViewModel : ViewModelBase
    {
        private readonly AddTrafficSplitWindow _owner;
        private bool _ipAddressSplit;
        private bool _cookieSplit;
        private string _selectedVersion;
        private int _allocation;

        public bool IpAddressSplit
        {
            get { return _ipAddressSplit; }
            set { SetValueAndRaise(ref _ipAddressSplit, value); }
        }

        public bool CookieSplit
        {
            get { return _cookieSplit; }
            set { SetValueAndRaise(ref _cookieSplit, value); }
        }

        public string SelectedVersion
        {
            get { return _selectedVersion; }
            set { SetValueAndRaise(ref _selectedVersion, value); }
        }

        public IEnumerable<string> Versions { get; }

        public int Allocation
        {
            get { return _allocation; }
            set { SetValueAndRaise(ref _allocation, value); }
        }

        public ICommand AddSplitCommand { get; }

        public AddTrafficSplitResult Result { get; private set; }

        public AddTrafficSplitViewModel(AddTrafficSplitWindow owner, IEnumerable<string> versions)
        {
            _owner = owner;

            Versions = versions;
            SelectedVersion = Versions.FirstOrDefault();
            AddSplitCommand = new ProtectedCommand(OnAddSplitCommand);
        }

        private void OnAddSplitCommand()
        {
            if (!ValidateInput())
            {
                UserPromptUtils.ErrorPrompt(
                    message: "Invalid input",
                    title: "Invalid input");
                return;
            }

            Result = new AddTrafficSplitResult(
                version: SelectedVersion,
                allocation: Allocation,
                ipAddressSplit: IpAddressSplit,
                cookieSplit: CookieSplit);
        }

        private bool ValidateInput()
        {
            throw new NotImplementedException();
        }
    }
}
