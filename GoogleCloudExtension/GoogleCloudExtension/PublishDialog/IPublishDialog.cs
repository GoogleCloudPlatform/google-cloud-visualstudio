using EnvDTE;

namespace GoogleCloudExtension.PublishDialog
{
    public interface IPublishDialog
    {
        Project Project { get; }

        void PushStep(IPublishDialogStep step);

        void Finished();
    }
}
