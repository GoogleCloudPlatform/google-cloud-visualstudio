using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.PublishDialogTargets.Gce
{
    public class GceTargetViewModel : ViewModelBase, IPublishDialogTarget
    {
        #region IPublishDialogTarget

        void IPublishDialogTarget.Publish()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
