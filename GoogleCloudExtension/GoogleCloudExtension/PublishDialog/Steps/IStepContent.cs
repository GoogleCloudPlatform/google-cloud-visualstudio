namespace GoogleCloudExtension.PublishDialog.Steps {
    public interface IStepContent<out T> where T : IPublishDialogStep
    {
        T ViewModel { get; }
    }
}