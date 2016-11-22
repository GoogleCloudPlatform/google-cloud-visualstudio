using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace GoogleCloudExtension.AddTrafficSplit
{
    public class AddTrafficSplitViewModel : ViewModelBase
    {
        private readonly AddTrafficSplitWindow _owner;
        private string _selectedVersion;
        private string _allocation = "0";

        public string SelectedVersion
        {
            get { return _selectedVersion; }
            set { SetValueAndRaise(ref _selectedVersion, value); }
        }

        public IEnumerable<string> Versions { get; }

        public string Allocation
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
            if (!Validate())
            {
                return;
            }

            Result = new AddTrafficSplitResult(
                version: SelectedVersion,
                allocation: Int32.Parse(Allocation));
            _owner.Close();
        }

        private bool Validate()
        {
            int allocationValue = 0;
            if (!Int32.TryParse(Allocation, out allocationValue))
            {
                UserPromptUtils.ErrorPrompt(
                    message: $"Invalid value for allocation, must be a number: {Allocation}",
                    title: "Invalid Value");
                return false;
            }

            if (allocationValue > 100 || allocationValue < 0)
            {
                UserPromptUtils.ErrorPrompt(
                    message: $"Invalid allocation value, must be between 0 to 100: {Allocation}",
                    title: "Invalid Value");
                return false;
            }

            return true;
        }
    }
}
