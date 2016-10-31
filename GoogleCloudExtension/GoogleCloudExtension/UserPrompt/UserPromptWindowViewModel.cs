using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace GoogleCloudExtension.UserPrompt
{
    public class UserPromptWindowViewModel : ViewModelBase
    {
        private readonly UserPromptWindow _owner;
        private readonly UserPromptWindow.Options _options;

        public string Prompt => _options.Prompt;

        public string Message => _options.Message;

        public ImageSource Icon => _options.Icon;

        public ICommand ActionCommand { get; }

        public string ActionButtonCaption => _options.ActionButtonCaption;

        public string CancelButtonCaption => _options.CancelButtonCaption;

        public bool Result { get; private set; }

        public UserPromptWindowViewModel(UserPromptWindow owner, UserPromptWindow.Options options)
        {
            _owner = owner;
            _options = options;

            ActionCommand = new ProtectedCommand(OnActionCommand);
        }

        private void OnActionCommand()
        {
            Result = true;
            _owner.Close();
        }
    }
}
