using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoogleCloudExtension.NamePrompt
{
    public class NamePromptViewModel : ViewModelBase
    {
        private readonly NamePromptWindow _owner;
        private string _name;

        public string Name
        {
            get { return _name; }
            set { SetValueAndRaise(ref _name, value); }
        }

        public ICommand OkCommand { get; }

        public NamePromptViewModel(NamePromptWindow owner)
        {
            _owner = owner;

            OkCommand = new ProtectedCommand(OnOkCommand);
        }

        private void OnOkCommand()
        {
            if (!Validate())
            {
                return;
            }

            _owner.Close();
        }

        private bool Validate()
        {
            if (String.IsNullOrEmpty(Name))
            {
                UserPromptUtils.ErrorPrompt("Name cannot be empty.", "Error");
                return false;
            }

            if (Name.Contains('/'))
            {
                UserPromptUtils.ErrorPrompt("Name cannot contain invalid chars '/'", "Error");
                return false;
            }

            return true;
        }
    }
}
