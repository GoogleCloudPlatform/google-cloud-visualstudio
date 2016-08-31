using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace GoogleCloudExtension.PublishDialogSteps.ChoiceStep
{
    public class Choice : Model
    {
        public string Name { get; set; }

        public string ToolTip { get; set; }

        public ICommand Command { get; set; }

        public ImageSource Icon { get; set; }
    }
}
