using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace GoogleCloudExtension.CloudExplorer
{
    public class ButtonDefinition : Model
    {
        private string _toolTip;
        private ImageSource _icon;
        private ICommand _command;
        private bool _isChecked;

        public string ToolTip
        {
            get { return _toolTip; }
            set { SetValueAndRaise(ref _toolTip, value); }
        }

        public ImageSource Icon
        {
            get { return _icon; }
            set { SetValueAndRaise(ref _icon, value); }
        }

        public ICommand Command
        {
            get { return _command; }
            set { SetValueAndRaise(ref _command, value); }
        }

        public bool IsChecked
        {
            get { return _isChecked; }
            set { SetValueAndRaise(ref _isChecked, value); }
        }
    }
}
