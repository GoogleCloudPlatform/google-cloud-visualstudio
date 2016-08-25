using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoogleCloudExtension.PublishDialogSteps.ChoiceStep
{
    public class Choice : Model
    {
        public string Name { get; set; }

        public ICommand Command { get; set; }
    }
}
