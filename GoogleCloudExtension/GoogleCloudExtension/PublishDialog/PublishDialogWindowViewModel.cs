using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.PublishDialog
{
    public class PublishDialogWindowViewModel : ViewModelBase
    {
        private readonly PublishDialogWindow _owner;

        public PublishDialogWindowViewModel(PublishDialogWindow owner)
        {
            _owner = owner;
        }
    }
}
