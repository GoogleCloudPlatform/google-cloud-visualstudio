using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.PublishDialog
{
    public interface IPublishDialog
    {
        Project Project { get; }

        void PushStep(IPublishDialogStep step);

        void Finished();
    }
}
