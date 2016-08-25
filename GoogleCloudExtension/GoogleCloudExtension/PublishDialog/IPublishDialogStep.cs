using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoogleCloudExtension.PublishDialog
{
    public interface IPublishDialogStep
    {
        bool CanGoNext { get; }

        bool CanPublish { get; }

        FrameworkElement Content { get; }

        IPublishDialogStep Next();

        void Publish();
    }
}
