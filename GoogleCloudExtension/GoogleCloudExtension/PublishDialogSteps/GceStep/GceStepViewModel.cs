using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoogleCloudExtension.PublishDialogSteps.GceStep
{
    public class GceStepViewModel : ViewModelBase, IPublishDialogStep
    {
        #region IPublishDialogTarget

        bool IPublishDialogStep.CanGoNext
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool IPublishDialogStep.CanPublish
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        FrameworkElement IPublishDialogStep.Content
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        IPublishDialogStep IPublishDialogStep.Next()
        {
            throw new NotImplementedException();
        }

        void IPublishDialogStep.Publish()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
