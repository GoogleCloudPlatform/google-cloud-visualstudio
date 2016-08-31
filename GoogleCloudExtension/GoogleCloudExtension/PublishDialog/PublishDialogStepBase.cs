using GoogleCloudExtension.Utils;
using System;
using System.Windows;

namespace GoogleCloudExtension.PublishDialog
{
    /// <summary>
    /// This is the base class for all step implementation, providing default implementations
    /// for the <seealso cref="IPublishDialogStep"/> interface.
    /// </summary>
    public abstract class PublishDialogStepBase : ViewModelBase, IPublishDialogStep
    {
        private bool _canGoNext;
        private bool _canPublish;

        public bool CanGoNext
        {
            get { return _canGoNext; }
            protected set
            {
                if (_canGoNext != value)
                {
                    _canGoNext = value;
                    CanGoNextChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool CanPublish
        {
            get { return _canPublish; }
            protected set
            {
                if (_canPublish != value)
                {
                    _canPublish = value;
                    CanPublishChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public abstract FrameworkElement Content { get; }

        public event EventHandler CanGoNextChanged;

        public event EventHandler CanPublishChanged;

        public virtual IPublishDialogStep Next()
        {
            throw new NotImplementedException();
        }

        public virtual void Publish()
        {
            throw new NotImplementedException();
        }

        public virtual void OnPushedToDialog(IPublishDialog dialog)
        { }
    }
}
