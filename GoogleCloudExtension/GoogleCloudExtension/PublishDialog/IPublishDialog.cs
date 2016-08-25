using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.PublishDialog
{
    public interface IPublishDialog
    {
        void PushStep(IPublishDialogStep step);
    }
}
